using Microsoft.Extensions.Logging;

using RedMaple.Orchestrator.Contracts.Deployments;
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
        private readonly ILogger<ClusterResourceManager> _logger;
        private readonly IClusterResourceRepository _repo;
        private bool _isInitialized = false;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);

        public ClusterResourceManager(ILogger<ClusterResourceManager> logger, IClusterResourceRepository repo)
        {
            _logger = logger;
            _repo = repo;
        }

        public async Task<List<ClusterResource>> GetClusterResourcesAsync(Dictionary<string, string> environmentVariables, string? region)
        {
            var assignedResources = new List<ClusterResource>();
            //var missingEnvironmentVariables = environmentVariables.Where(x => string.IsNullOrEmpty(x.Value)).Select(x => x.Key).ToList();

            // Find cluster resources that define the variable
            //if (missingEnvironmentVariables.Count > 0)
            {
                var resources = await GetClusterResourcesAsync();
                var matched = new HashSet<string>();
                foreach (var envVar in environmentVariables.Keys)
                {
                    // First match the region
                    foreach (var resource in resources)
                    {
                        if (resource.EnvironmentVariables.ContainsKey(envVar) && region == resource.ResourceRegion && !matched.Contains(envVar))
                        {
                            matched.Add(envVar);
                            if (!assignedResources.Contains(resource))
                            {
                                _logger.LogInformation("Assigning resource {resourceName} (region={region}) to plan", resource.Name, resource.ResourceRegion);
                                assignedResources.Add(resource);
                            }
                            break;
                        }
                    }

                    // Secoind match global resource
                    foreach (var resource in resources)
                    {
                        if (resource.EnvironmentVariables.ContainsKey(envVar) && resource.ResourceRegion is null && !matched.Contains(envVar))
                        {
                            matched.Add(envVar);
                            if (!assignedResources.Contains(resource))
                            {
                                _logger.LogInformation("Assigning resource {resourceName} (region={region}) to plan", resource.Name, resource.ResourceRegion);
                                assignedResources.Add(resource);
                            }
                            break;
                        }
                    }

                    // Second, any resource 
                    foreach (var resource in resources)
                    {
                        if (resource.EnvironmentVariables.ContainsKey(envVar) && !matched.Contains(envVar))
                        {
                            matched.Add(envVar);
                            if (!assignedResources.Contains(resource))
                            {
                                _logger.LogInformation("Assigning resource {resourceName} (region={region}) to plan", resource.Name, resource.ResourceRegion);
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
            await _lock.WaitAsync();
            try
            {
                await EnsureInitializedAsync();
                var resources = _resources.Values.ToList();
                return resources;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task AddResourceAsync(ClusterResource resource)
        {
            await _lock.WaitAsync();
            try
            {
                await EnsureInitializedAsync();
                _resources[resource.Id] = resource;
                if (resource.Persist)
                {
                    await _repo.SaveResourceAsync(resource);
                }
            }
            finally
            {
                _lock.Release();
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
            await _lock.WaitAsync();
            try
            {
                await EnsureInitializedAsync();
                _resources.TryRemove(resource.Id, out _);
                if (resource.Persist)
                {
                    await _repo.DeleteResourceAsync(resource);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task SaveResourceAsync(ClusterResource resource)
        {
            await _lock.WaitAsync();
            try
            {
                await EnsureInitializedAsync();
                _resources[resource.Id] = resource;
                if (resource.Persist)
                {
                    await _repo.SaveResourceAsync(resource);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<List<ClusterResource>> GetClusterResourcesByPlanAsync(string planId)
        {
            await _lock.WaitAsync();
            try
            {
                await EnsureInitializedAsync();
                return _resources.Values.Where(x => x.PlanId == planId).ToList();
            }
            finally
            {
                _lock.Release();
            }
        }
        public async Task<ClusterResource?> GetClusterResourceAsync(string id)
        {
            await _lock.WaitAsync();
            try
            {
                await EnsureInitializedAsync();
                _resources.TryGetValue(id, out var resource);
                return resource;
            }
            finally
            {
                _lock.Release();
            }
        }

    }
}