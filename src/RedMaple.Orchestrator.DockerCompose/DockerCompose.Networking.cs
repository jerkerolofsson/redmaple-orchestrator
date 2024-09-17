﻿using Docker.DotNet.Models;
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

        private async Task<List<NetworkResponse>> CreateNetworksAsync(DockerComposePlan plan, CancellationToken cancellationToken)
        {
            var ret = new List<NetworkResponse>();

            foreach (var pair in plan.networks)
            {
                var name = pair.Key;
                var network = pair.Value;
                var existingNetwork = await FindProjectNetwork(plan, name, cancellationToken);
                if(existingNetwork is null)
                {
                    await _docker.CreateNetworkAsync(new NetworksCreateParameters
                    {
                        Name = name,
                        Driver = network.driver,
                        Internal = network.Internal ?? true,
                        Scope = network.scope,
                        Labels = new Dictionary<string, string>
                        {
                            [DockerComposeConstants.LABEL_PROJECT] = plan.ProjectName
                        }
                    }, cancellationToken);

                    ret.Add(await FindProjectNetwork(plan, name, cancellationToken) ?? throw new Exception("Failed to create network"));
                }
                else
                {
                    ret.Add(existingNetwork);
                }
            }

            return ret;
        }

        private async Task<NetworkResponse?> CreateDefaultNetworkAsync(DockerComposePlan plan, CancellationToken cancellationToken)
        {
            var defaultNetworkName = plan.name + "_default";
            var existingNetwork = await FindProjectNetwork(plan, defaultNetworkName, cancellationToken);
            if (existingNetwork is null)
            {
                await _docker.CreateNetworkAsync(new NetworksCreateParameters
                {
                    Name = defaultNetworkName,
                    Driver = "bridge",
                    Labels = new Dictionary<string,string>
                    {
                        [DockerComposeConstants.LABEL_PROJECT] = plan.ProjectName
                    }
                }, cancellationToken);

                existingNetwork = await FindProjectNetwork(plan, defaultNetworkName, cancellationToken) 
                    ?? throw new Exception("Failed to find default network after creating it");
            }

            return existingNetwork;
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
    }
}
