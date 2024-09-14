using Docker.DotNet.Models;
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
            await DownContainersAsync(progress, plan, cancellationToken);

            plan = TransformPlanWithEnvironmentVariables(plan, environmentFile);

            progress.Report($"Creating default docker network..");
            var network = await CreateDefaultNetworkAsync(plan, cancellationToken);
            if(network is null)
            {
                throw new Exception("Failed to create network");
            }

            var containerIds = await CreateContainersAsync(plan, network, environmentFile);
            foreach(var containerId in containerIds)
            {
                await _docker.StartAsync(containerId, cancellationToken);
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
            return transformedPlan;
        }

        private async Task<List<string>> CreateContainersAsync(DockerComposePlan plan, NetworkResponse network, string? environmentFile)
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
