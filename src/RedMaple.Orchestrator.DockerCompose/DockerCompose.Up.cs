using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.Contracts.Deployments;
using RedMaple.Orchestrator.Contracts.Resources;
using RedMaple.Orchestrator.DockerCompose.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.DockerCompose
{
    public partial class DockerCompose
    {

        public async Task UpAsync(IProgress<string> progress, DockerComposePlan plan, string? environmentFile, CancellationToken cancellationToken)
        {
            if(string.IsNullOrWhiteSpace(plan.name))
            {
                throw new ArgumentException("Plan name is missing", nameof(plan.name));
            }

            try
            {
                await DownContainersAsync(progress, plan, cancellationToken);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error tearing down containers when up:ing plan: {DockerComposePlanName}", plan.name);
            }

            plan = TransformPlanWithEnvironmentVariables(plan, environmentFile);

            progress.Report($"Creating default network: {plan.name}_default..");
            var defaultNetwork = await CreateDefaultNetworkAsync(plan, cancellationToken);
            if(defaultNetwork is null)
            {
                throw new Exception("Failed to create network");
            }

            progress.Report("Creating volumes..");
            await CreateVolumesAsync(plan, cancellationToken);

            var containerIds = await CreateContainersAsync(plan, defaultNetwork, environmentFile);

            // Create additional networks
            progress.Report("Creating networks..");
            var networks = await CreateNetworksAsync(plan, cancellationToken);
            await ConnectContainersAsync(plan, networks, cancellationToken);

            // Start the containers
            foreach (var containerId in containerIds)
            {
                await _docker.StartAsync(containerId, cancellationToken);
            }
        }

        private async Task CreateVolumesAsync(DockerComposePlan plan, CancellationToken cancellationToken)
        {
            foreach(var pair in plan.volumes)
            {
                var name = pair.Key;
                DockerComposeVolume composeVolume = pair.Value;

                var volumeCreateParameters = new VolumesCreateParameters
                {
                    Name = name,
                    Driver = composeVolume.driver ?? "local",
                    DriverOpts = composeVolume.driver_opts,
                    Labels = new Dictionary<string, string>
                    {
                        [DockerComposeConstants.LABEL_PROJECT] = plan.ProjectName
                    }
                };

                await _volumes.TryCreateVolumeAsync(volumeCreateParameters, cancellationToken);
            }
        }

        private async Task ConnectContainersAsync(DockerComposePlan plan, List<NetworkResponse> networks, CancellationToken cancellationToken)
        {
            if(plan.services is null)
            {
                return;
            }
            foreach(var service in plan.services.Values)
            {
                if(service.networks is null || service.container_name is null)
                {
                    continue;
                }

                // Find the container 
                var container = await FindContainerAsync(plan, service.container_name, cancellationToken);
                if(container is null)
                {
                    throw new Exception($"Internal Error, cannot find container '{service.container_name}'");
                }

                foreach (var networkName in service.networks)
                {
                    var network = networks.Where(x=>x.Name ==  networkName).FirstOrDefault();
                    if(network is null)
                    {
                        throw new ArgumentException($"Network '{networkName}' was not found");
                    }
                    await _docker.ConnectNetworkAsync(network.ID, new NetworkConnectParameters
                    {
                        Container = container.Id
                    }, cancellationToken);
                }
            }
        }

        private DockerComposePlan TransformPlanWithEnvironmentVariables(DockerComposePlan plan, string? environmentFile)
        {
            Dictionary<string, string?> env = new();
            Dictionary<string, string?> envFile = LoadEnvironmentFile(plan, environmentFile);
            AssignEnvironmentVariables(plan.RequiredEnvironmentVariables??new List<string>(), null, envFile, env, validate:false);

            var transformedYaml = DockerComposeParser.ReplaceEnvironmentVariables(plan.Yaml, env);
            var transformedPlan = DockerComposeParser.ParseYaml(transformedYaml);
            transformedPlan.RequiredEnvironmentVariables = plan.RequiredEnvironmentVariables;
            transformedPlan.Labels = plan.Labels;
            transformedPlan.name = plan.name;
            return transformedPlan;
        }

        private async Task<List<string>> CreateContainersAsync(
            DockerComposePlan plan, 
            NetworkResponse network, 
            string? environmentFile)
        {
            var containerIds = new List<string>();

            Dictionary<string, string?> envFile = LoadEnvironmentFile(plan, environmentFile);
            int containerNumber = 1;
            foreach (var servicePair in plan.services)
            {
                var name = servicePair.Key;
                var service = servicePair.Value;
                if (service is null)
                {
                    continue;
                }

                service.container_name ??= name;

                Dictionary<string, string?> env = new();
                if (plan.RequiredEnvironmentVariables is not null)
                {
                    AssignEnvironmentVariables(
                        plan.RequiredEnvironmentVariables,
                        service.environment,
                        envFile,
                        target: env);
                }


                var containerLabels = new Dictionary<string, string>(plan.Labels);
                containerLabels["com.docker.compose.container-number"] = containerNumber.ToString();
                containerLabels["com.docker.compose.service"] = name;

                var networkingConfig = new NetworkingConfig
                {
                    EndpointsConfig = new Dictionary<string, EndpointSettings>
                    {
                        [network.Name] = new EndpointSettings { NetworkID = network.ID }
                    }
                };

                var containerResponse = await _docker.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = service.image,
                    Name = service.container_name,
                    Hostname = service.hostname,
                    Domainname = service.domainname,
                    NetworkingConfig = networkingConfig,
                    Volumes = MapVolumes(service),
                    ExposedPorts = GetExposedPorts(service),
                    HostConfig = CreateHostConfig(service),
                    Env = MapEnv(env),
                    Labels = containerLabels,
                });
                containerNumber++;
                containerIds.Add(containerResponse.ID);
            }
            return containerIds;
        }

        private IDictionary<string, EmptyStruct> MapVolumes(DockerComposeService service)
        {
            Dictionary<string, EmptyStruct> volumes = new();
            foreach (var volume in service.volumes)
            {
                volumes[volume.ToShortFormat(includeAccessMode:false)] = new EmptyStruct();
            }
            return volumes;
        }

        private IDictionary<string, EmptyStruct> GetExposedPorts(DockerComposeService service)
        {
            Dictionary<string, EmptyStruct> ports = new();
            if (service.ports is not null)
            {
                foreach (var port in service.ports)
                {
                    string key = $"{port.ContainerPort}/{port.Protocol ?? "tcp"}";
                    ports[key] = new EmptyStruct();
                }
            }
            return ports;
        }

        private HostConfig CreateHostConfig(DockerComposeService service)
        {
            return new HostConfig
            {
                AutoRemove = false,
                DNS = service.dns,
                DNSOptions = service.dns_opt,
                DNSSearch = service.dns_search,
                CapAdd = service.cap_add,
                RestartPolicy = (service.restart ?? "unless-stopped") switch
                {
                    "unless-stopped" => new RestartPolicy() { Name = RestartPolicyKind.UnlessStopped },
                    "no" => new RestartPolicy() { Name = RestartPolicyKind.No },
                    "always" => new RestartPolicy() { Name = RestartPolicyKind.Always },
                    "on-failure" => new RestartPolicy() { Name = RestartPolicyKind.OnFailure },
                    _ => new RestartPolicy() { Name = RestartPolicyKind.No },
                },
                PortBindings = CreatePortBindings(service),
                Binds = CreateBinds(service)
            };
        }

        private IDictionary<string, IList<PortBinding>> CreatePortBindings(DockerComposeService service)
        {
            var portBindings = new Dictionary<string, IList<PortBinding>>();
            if (service.ports is not null)
            {
                foreach (var port in service.ports)
                {
                    // Default to same port unless excplicitly defined
                    var hostPort = port.HostPort ?? port.ContainerPort;

                    var key = $"{port.ContainerPort}/{port.Protocol ?? "tcp"}";
                    if(!portBindings.ContainsKey(key))
                    {
                        portBindings[key] = new List<PortBinding>();
                    }
                    portBindings[key].Add(new PortBinding
                    {
                        HostIP = port.HostIp,
                        HostPort = hostPort.ToString()
                    });
                }
            }
            return portBindings;
        }

        // Volume binds
        private IList<string> CreateBinds(DockerComposeService service)
        {
            var binds = new List<string>();
            foreach(var bind in service.volumes)
            {
                binds.Add(bind.ToShortFormat());
            }
            return binds;
        }
    }
}
