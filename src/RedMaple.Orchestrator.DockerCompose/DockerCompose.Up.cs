﻿using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;

using RedMaple.Orchestrator.Contracts.Containers;
using RedMaple.Orchestrator.Contracts.Deployments;
using RedMaple.Orchestrator.Contracts.Resources;
using RedMaple.Orchestrator.DockerCompose.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RedMaple.Orchestrator.DockerCompose
{
    public partial class DockerCompose
    {

        public async Task UpAsync(IProgress<string> progress, DockerComposePlan plan, string? environmentFile, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(plan.name))
            {
                throw new ArgumentException("Plan name is missing", nameof(plan.name));
            }

            try
            {
                await DownContainersAsync(progress, plan, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tearing down containers when up:ing plan: {DockerComposePlanName}", plan.name);
            }

            _logger.LogDebug("Transforming plan: {PlanName} with {ServiceCount} services..", plan.name, plan.services.Count);
            plan = TransformPlanWithEnvironmentVariables(plan, environmentFile);
            _logger.LogDebug("Transformed plan: {PlanName} with {ServiceCount} services..", plan.name, plan.services.Count);

            progress.Report($"Creating default docker network: {plan.name}_default..");
            NetworkResponse? defaultNetwork = null;
            if (plan.networks is null || plan.networks.Count == 0)
            {
                _logger.LogInformation("Creating default network..");
                defaultNetwork = await CreateDefaultNetworkAsync(plan, cancellationToken);
                if (defaultNetwork is null)
                {
                    _logger.LogError("Failed to create network.");
                    throw new Exception("Failed to create network");
                }
            }

            progress.Report("Creating docker volumes..");
            await CreateVolumesAsync(plan, cancellationToken);

            progress.Report("Connecting containers..");
            var containerIds = await CreateContainersAsync(plan, defaultNetwork, environmentFile);
            _logger.LogInformation("Created {Count} containers for plan {PlanName}: {ContainerIds}", containerIds.Count, plan.name, string.Join(",",containerIds));

            // Create additional networks
            progress.Report("Creating docker networks..");
            var networks = await CreateNetworksAsync(plan, cancellationToken);

            progress.Report("Connecting containers to networks..");
            await ConnectContainersAsync(plan, networks, cancellationToken);

            // Start the containers
            foreach (var containerId in containerIds)
            {
                progress.Report($"Starting container {containerId}..");
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
                    Driver = composeVolume?.driver ?? "local",
                    DriverOpts = composeVolume?.driver_opts,
                    Labels = new Dictionary<string, string>
                    {
                        [DockerComposeConstants.LABEL_PROJECT] = plan.ProjectName
                    }
                };

                var createVolumeResult = await _volumes.TryCreateVolumeAsync(volumeCreateParameters, cancellationToken);
                if(createVolumeResult == CreateVolumeResult.Error)
                {
                    throw new Exception("Failed to create volume!");
                }
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
                        network = await FindNetwork(plan, networkName, cancellationToken);
                        if(network is null)
                        {
                            throw new ArgumentException($"Network '{networkName}' was not found (not defined in compose file and no existing network with that name was found)");
                        }
                    }
                    if (network.Name == "host")
                    {
                        _logger.LogDebug("Connecting container {ContainerName} cannot be connected to network {NetworkName}, skipping..", service.container_name, network.Name);
                    }
                    else
                    {
                        _logger.LogDebug("Connecting container {ContainerName} to network {NetworkName}..", service.container_name, network.Name);
                        await _docker.ConnectNetworkAsync(network.ID, new NetworkConnectParameters
                        {
                            Container = container.Id
                        }, cancellationToken);
                    }
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
            NetworkResponse? network, 
            string? environmentFile)
        {
            _logger.LogInformation("Creating {ContainerCount} containers..", plan.services.Count);
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
                _logger.LogDebug("Creating container #{ContainerNumber} {ServiceName} with image, {DockerImage}..", containerNumber, servicePair.Value, service.image);

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
                _logger.LogDebug("Creating container, com.docker.compose.service={DockerComposeServiceName}", name);

                var primaryServiceNetwork = network;
                if (service.network_mode == "host")
                {
                    _logger.LogDebug("network_mode is 'host', using host network for container {ContainerName}", service.container_name);
                    primaryServiceNetwork = await FindNetwork(plan, "host", CancellationToken.None);
                }
                else if ((service.networks is not null && service.networks.Where(x => x == "host").Any()))
                {
                    // We cannot connect to host network, so we'll set it here
                    _logger.LogDebug("network is 'host', using host network for container {ContainerName}", service.container_name);
                    primaryServiceNetwork = await FindNetwork(plan, "host", CancellationToken.None);
                    if(service.networks.Count > 1)
                    {
                        throw new Exception($"Container {service.container_name} uses 'host' network and cannot use additional networks");
                    }
                }

                var networkingConfig = new NetworkingConfig
                {
                    
                };
                if(primaryServiceNetwork is not null)
                {
                    networkingConfig.EndpointsConfig = new Dictionary<string, EndpointSettings>
                    {
                        [primaryServiceNetwork.Name] = new EndpointSettings { NetworkID = primaryServiceNetwork.ID }
                    };
                }

                var containerResponse = await _docker.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = service.image,
                    Name = service.container_name,
                    Hostname = service.hostname ?? name ?? service.container_name,
                    Domainname = service.domainname,
                    Cmd = service.command == null ? [] : service.command,
                    NetworkingConfig = networkingConfig,
                    Volumes = MapVolumes(service),
                    ExposedPorts = GetExposedPorts(service),
                    HostConfig = CreateHostConfig(service),
                    Env = MapEnv(env),
                    Labels = containerLabels,
                });

                containerNumber++;
                if (containerResponse is null)
                {
                    _logger.LogError("Got null response after creating container!");
                }
                else
                {
                    _logger.LogInformation("Created container {ContainerId}, image={DockerImage}", containerResponse.ID, service.image);
                    if(containerResponse.Warnings is not null)
                    {
                        foreach(var warning in containerResponse.Warnings)
                        {
                            _logger.LogWarning(warning);
                        }
                    }
                    containerIds.Add(containerResponse.ID);
                }
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
                    if (port.ContainerPort is null)
                    {
                        continue;
                    }
                    if (port.ContainerPort.Contains('-'))
                    {
                        var containerPortRange = port.ContainerPort.Split('-');
                        var hostPortRange = containerPortRange;
                        int containerStart, containerEnd, hostStart, hostEnd;
                        ValidatePortRange(containerPortRange, hostPortRange, out containerStart, out containerEnd, out hostStart, out hostEnd);

                        for(int i=containerStart; i<=containerEnd; i++)
                        {
                            string key = $"{i}/{port.Protocol ?? "tcp"}";
                            ports[key] = new EmptyStruct();
                        }
                    }
                    else
                    {
                        string key = $"{port.ContainerPort}/{port.Protocol ?? "tcp"}";
                        ports[key] = new EmptyStruct();
                    }
                }
            }
            return ports;
        }

        private HostConfig CreateHostConfig(DockerComposeService service)
        {
            _logger.LogInformation("Creating host config, dns={DNS}, privileged={privileged}, restart={RestartPolicy}, network_mode={network_mode}", service.dns, service.privileged, service.restart, service.network_mode);
            
            return new HostConfig
            {
                AutoRemove = false,
                DNS = service.dns,
                DNSOptions = service.dns_opt,
                DNSSearch = service.dns_search,
                CapAdd = service.cap_add,
                NetworkMode = service.network_mode,
                DeviceRequests = CreateDevicesRequest(service),
                Privileged = service.privileged,
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

        private IList<DeviceRequest> CreateDevicesRequest(DockerComposeService service)
        {
            var devices = new List<DeviceRequest>();
            if(service.deploy?.resources?.reservations?.devices is not null)
            {
                foreach(var device in service.deploy.resources.reservations.devices)
                {
                    var caps = new List<IList<string>>();
                    if(device.capabilities is not null)
                    {
                        caps.Add(new List<string>(device.capabilities));
                    }

                    devices.Add(new DeviceRequest
                    {
                        Capabilities = caps,
                        Count = device.count ?? 1,
                        Driver = device.driver,
                        Options = device.options,
                        DeviceIDs = device.device_ids
                    });
                }
            }
            return devices;
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

                    if(hostPort is null || port.ContainerPort is null)
                    {
                        continue;
                    }

                    if (port.ContainerPort.Contains('-') && hostPort.Contains('-'))
                    {
                        var containerPortRange = port.ContainerPort.Split('-');
                        var hostPortRange = hostPort.Split('-');
                        int containerStart, containerEnd, hostStart, hostEnd;
                        ValidatePortRange(containerPortRange, hostPortRange, out containerStart, out containerEnd, out hostStart, out hostEnd);

                        var containerRange = containerEnd - containerStart + 1;
                        var hostRange = hostEnd - hostStart + 1;
                        if (containerRange != hostRange)
                        {
                            throw new Exception("Container port range must match host range");
                        }
                        if (containerRange <= 0 || hostRange <= 0)
                        {
                            throw new Exception("Invalid port range (start>end)");
                        }

                        var currentContainer = containerStart;
                        var currentHost = hostStart;
                        for (int i = 0; i < containerRange; i++)
                        {
                            AddPortMapping(portBindings, port, currentHost.ToString(), currentContainer.ToString());
                            currentContainer++;
                            currentHost++;
                        }
                    }
                    else
                    {
                        var containerPort = port.ContainerPort;
                        AddPortMapping(portBindings, port, hostPort, containerPort);
                    }
                }
            }
            return portBindings;
        }

        private static void ValidatePortRange(string[] containerPortRange, string[] hostPortRange, out int containerStart, out int containerEnd, out int hostStart, out int hostEnd)
        {
            if (containerPortRange.Length != 2 || hostPortRange.Length != 2)
            {
                throw new Exception("Expected port range two container a single -");
            }
            if (!int.TryParse(containerPortRange[0], out containerStart))
            {
                throw new Exception("Expected integer in port range");
            }
            if (!int.TryParse(containerPortRange[1], out containerEnd))
            {
                throw new Exception("Expected integer in port range");
            }
            if (!int.TryParse(hostPortRange[0], out hostStart))
            {
                throw new Exception("Expected integer in port range");
            }
            if (!int.TryParse(hostPortRange[1], out hostEnd))
            {
                throw new Exception("Expected integer in port range");
            }
        }

        private static void AddPortMapping(Dictionary<string, IList<PortBinding>> portBindings, DockerComposePortMapping port, string? hostPort, string? containerPort)
        {
            var key = $"{containerPort}/{port.Protocol ?? "tcp"}";
            if (!portBindings.ContainsKey(key))
            {
                portBindings[key] = new List<PortBinding>();
            }
            portBindings[key].Add(new PortBinding
            {
                HostIP = port.HostIp,
                HostPort = hostPort ?? containerPort
            });
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
