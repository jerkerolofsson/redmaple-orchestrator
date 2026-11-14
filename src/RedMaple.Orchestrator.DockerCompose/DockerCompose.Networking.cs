using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.DockerCompose.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RedMaple.Orchestrator.DockerCompose
{
    public partial class DockerCompose
    {
        private async Task<NetworkResponse?> FindProjectNetwork(DockerComposePlan plan, string name, CancellationToken cancellationToken)
        {
            var networks = await _docker.ListNetworksAsync(new NetworksListParameters(), cancellationToken);
            foreach (var network in networks)
            {
                if(network.Labels is not null && 
                    network.Labels.TryGetValue(DockerComposeConstants.LABEL_PROJECT, out var networkProject))
                {
                    if(networkProject == plan.ProjectName && name == network.Name)
                    {
                        return network;
                    }
                }
            }
            return null;
        }

        private async Task<NetworkResponse?> FindNetwork(DockerComposePlan plan, string name, CancellationToken cancellationToken)
        {
            var networks = await _docker.ListNetworksAsync(new NetworksListParameters(), cancellationToken);
            foreach (var network in networks)
            {
                if (name == network.Name)
                {
                    return network;
                }
            }
            return null;
        }

        private async Task<List<NetworkResponse>> CreateNetworksAsync(DockerComposePlan plan, CancellationToken cancellationToken)
        {
            var ret = new List<NetworkResponse>();

            foreach (var pair in plan.networks)
            {
                var name = pair.Key;
                var network = pair.Value;

                // Handle external networks - they should already exist on the platform
                if (network.external == true)
                {
                    var networkName = network.name ?? name;
                    _logger.LogInformation("Network {NetworkName} marked as external, validating it exists..", networkName);
                    var externalNetwork = await FindNetwork(plan, networkName, cancellationToken);
                    if (externalNetwork is null)
                    {
                        throw new Exception($"External network '{networkName}' does not exist");
                    }
                    _logger.LogInformation("External network {NetworkName} found..", networkName);
                    ret.Add(externalNetwork);
                    continue;
                }

                // Use custom name if provided, otherwise use the dictionary key
                var finalNetworkName = network.name ?? name;
                var existingNetwork = await FindProjectNetwork(plan, finalNetworkName, cancellationToken);

                if(existingNetwork is null)
                {
                    // Merge user-defined labels with the project label
                    var labels = MergeLabels(
                        network.labels,
                        new Dictionary<string, string>
                        {
                            [DockerComposeConstants.LABEL_PROJECT] = plan.ProjectName
                        }
                    );

                    // Build network creation parameters
                    var networkParams = new NetworksCreateParameters
                    {
                        Name = finalNetworkName,
                        Driver = network.driver,
                        Internal = network.Internal ?? true,
                        Scope = network.scope,
                        Labels = labels,
                        Attachable = network.attachable ?? false,
                        EnableIPv6 = network.enable_ipv6 ?? false
                    };

                    // Add IPAM configuration if provided
                    if (network.ipam is not null)
                    {
                        networkParams.IPAM = CreateIPAMConfiguration(network.ipam);
                    }

                    await _docker.CreateNetworkAsync(networkParams, cancellationToken);

                    _logger.LogInformation("Creating network {NetworkName}, driver={NetworkDriver}..", finalNetworkName, network.driver);
                    ret.Add(await FindProjectNetwork(plan, finalNetworkName, cancellationToken) ?? throw new Exception($"Failed to create network '{finalNetworkName}'"));
                }
                else
                {
                    _logger.LogInformation("Existing network {NetworkName} found..", finalNetworkName);
                    ret.Add(existingNetwork);
                }
            }

            return ret;
        }

        private async Task<NetworkResponse?> CreateDefaultNetworkAsync(DockerComposePlan plan, CancellationToken cancellationToken)
        {
            if (plan.networks is null || plan.networks.Count == 0)
            {
                var defaultNetworkName = plan.name + "_default";
                _logger.LogInformation("Creating default network: {NetworkName}", defaultNetworkName);
                var existingNetwork = await FindProjectNetwork(plan, defaultNetworkName, cancellationToken);
                if (existingNetwork is null)
                {
                    _logger.LogDebug("No existing network with name {NetworkName} was found, creating a new one..", defaultNetworkName);
                    await _docker.CreateNetworkAsync(new NetworksCreateParameters
                    {
                        Name = defaultNetworkName,
                        Driver = "bridge",
                        Labels = new Dictionary<string, string>
                        {
                            [DockerComposeConstants.LABEL_PROJECT] = plan.ProjectName
                        }
                    }, cancellationToken);

                    existingNetwork = await FindProjectNetwork(plan, defaultNetworkName, cancellationToken)
                        ?? throw new Exception("Failed to find default network after creating it");
                }
                else
                {
                    _logger.LogDebug("An existing network with name {NetworkName} was already found..", defaultNetworkName);
                }

                return existingNetwork;
            }
            return null;
        }

        private async Task DeleteNetworksAsync(IProgress<string> progress, DockerComposePlan plan, CancellationToken cancellationToken)
        {
            var networks = await _docker.ListNetworksAsync(new NetworksListParameters(), cancellationToken);
            foreach(var network in networks)
            {
                if (network.Labels.TryGetValue(DockerComposeConstants.LABEL_PROJECT, out var project) &&
                    project == plan.ProjectName)
                {
                    progress.Report($"Deleting network {network.Name}..");

                    await _docker.DeleteNetworkAsync(network.ID, new ContainerRemoveParameters
                    {
                        Force = true
                    }, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Creates an IPAM configuration from DockerComposeNetworkIpam
        /// </summary>
        private IPAM CreateIPAMConfiguration(DockerComposeNetworkIpam ipamConfig)
        {
            var ipam = new IPAM
            {
                Driver = ipamConfig.driver ?? "default",
                Config = new List<IPAMConfig>(),
                Options = ipamConfig.options
            };

            if (ipamConfig.config is not null && ipamConfig.config.Count > 0)
            {
                foreach (var config in ipamConfig.config)
                {
                    ipam.Config.Add(new IPAMConfig
                    {
                        Subnet = config.subnet,
                        IPRange = config.ip_range,
                        Gateway = config.gateway,
                        AuxAddress = config.aux_addresses
                    });
                }
            }

            return ipam;
        }

        /// <summary>
        /// Merges two label dictionaries, with user-defined labels taking precedence over defaults
        /// </summary>
        private Dictionary<string, string> MergeLabels(
            Dictionary<string, string>? userLabels, 
            Dictionary<string, string> defaultLabels)
        {
            var result = new Dictionary<string, string>(defaultLabels);

            if (userLabels is not null && userLabels.Count > 0)
            {
                foreach (var label in userLabels)
                {
                    result[label.Key] = label.Value;
                }
            }

            return result;
        }
    }
}
