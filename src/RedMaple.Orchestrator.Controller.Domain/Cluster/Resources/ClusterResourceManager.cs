﻿using RedMaple.Orchestrator.Contracts.Deployments;
using RedMaple.Orchestrator.Contracts.Resources;
using RedMaple.Orchestrator.DockerCompose.Converters;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Domain.Cluster.Resources
{
    public class ClusterResourceManager : IClusterResourceManager
    {
        private readonly ConcurrentDictionary<string, ClusterResource> _resources = new();
        private readonly IClusterResourceRepository _repo;
        private bool _isInitialized = false;

        public ClusterResourceManager(IClusterResourceRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<ClusterResource>> GetClusterResourcesAsync(Dictionary<string,string> environmentVariables)
        {
            var assignedResources = new List<ClusterResource>();
            var missingEnvironmentVariables = environmentVariables.Where(x => string.IsNullOrEmpty(x.Value)).Select(x => x.Key).ToList();

            // Find cluster resources that define the variable
            if (missingEnvironmentVariables.Count > 0)
            {
                var resources = await GetClusterResourcesAsync();
                foreach (var envVar in missingEnvironmentVariables)
                {
                    foreach (var resource in resources)
                    {
                        if (resource.EnvironmentVariables.ContainsKey(envVar))
                        {
                            if (!assignedResources.Contains(resource))
                            {
                                assignedResources.Add(resource);
                            }
                            break;
                        }
                    }
                }

            }

            return assignedResources;
        }

        public async Task<List<ClusterResource>> GetClusterResourcesAsync()
        {
            await EnsureInitializedAsync();
            var resources = _resources.Values.ToList();
            return resources;
        }

        public async Task AddResourceAsync(ClusterResource resource)
        {
            await EnsureInitializedAsync();
            _resources[resource.Id] = resource;
            if(resource.Persist)
            {
                await _repo.SaveResourceAsync(resource);    
            }
        }

        private async Task EnsureInitializedAsync()
        {
            if (!_isInitialized)
            {
                foreach (var persistantResource in await _repo.GetResourcesAsync())
                {
                    _resources[persistantResource.Id] = persistantResource;
                }
                _isInitialized = true;
            }
        }

        public async Task RemoveResourceAsync(ClusterResource resource)
        {
            await EnsureInitializedAsync();
            _resources.TryRemove(resource.Id, out _);
            if(resource.Persist)
            {
                await _repo.DeleteResourceAsync(resource);
            }
        }

        public async Task SaveResourceAsync(ClusterResource resource)
        {
            await EnsureInitializedAsync();
            _resources[resource.Id] = resource;
            if (resource.Persist)
            {
                await _repo.SaveResourceAsync(resource);
            }
        }

        public async Task<ClusterResource?> GetClusterResourceAsync(string id)
        {
            await EnsureInitializedAsync();
            _resources.TryGetValue(id, out var resource);
            return resource;
        }
    }
}
