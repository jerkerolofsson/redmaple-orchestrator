using RedMaple.Orchestrator.Contracts.Resources;
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

        public Task<List<ClusterResource>> GetClusterResourcesAsync()
        {
            var resources = _resources.Values.ToList();
            return Task.FromResult(resources);
        }

        public Task AddResourceAsync(ClusterResource resource)
        {
            _resources[resource.Id] = resource;
            return Task.CompletedTask;
        }

        public Task RemoveResourceAsync(ClusterResource resource)
        {
            _resources.TryRemove(resource.Id, out _);
            return Task.CompletedTask;
        }

        public Task SaveResourceAsync(ClusterResource resource)
        {
            _resources[resource.Id] = resource;
            return Task.CompletedTask;
        }

        public Task<ClusterResource?> GetClusterResourceAsync(string id)
        {
            _resources.TryGetValue(id, out var resource);
            return Task.FromResult(resource);
        }
    }
}
