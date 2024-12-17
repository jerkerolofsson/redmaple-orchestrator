using Microsoft.Extensions.Logging;

using RedMaple.Orchestrator.Contracts.Deployments;
using RedMaple.Orchestrator.Contracts.Infra;
using RedMaple.Orchestrator.Contracts.Resources;
using RedMaple.Orchestrator.DockerCompose.Converters;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Domain.Cluster.Infra
{
    public class InfraResourceManager : IInfraResourceManager
    {
        private readonly ConcurrentDictionary<string, InfraResource> _resources = new();
        private readonly ILogger<InfraResourceManager> _logger;
        private readonly IInfraRepository _repo;
        private bool _isInitialized = false;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);

        public InfraResourceManager(ILogger<InfraResourceManager> logger, IInfraRepository repo)
        {
            _logger = logger;
            _repo = repo;
        }

        public async Task<List<InfraResource>> GetInfraResourcesAsync()
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

        public async Task AddResourceAsync(InfraResource resource)
        {
            await _lock.WaitAsync();
            try
            {
                await EnsureInitializedAsync();
                _resources[resource.Id] = resource;
                await _repo.SaveResourceAsync(resource);
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

        public async Task RemoveResourceAsync(InfraResource resource)
        {
            await _lock.WaitAsync();
            try
            {
                await EnsureInitializedAsync();
                _resources.TryRemove(resource.Id, out _);
                await _repo.DeleteResourceAsync(resource);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task SaveResourceAsync(InfraResource resource)
        {
            await _lock.WaitAsync();
            try
            {
                await EnsureInitializedAsync();
                _resources[resource.Id] = resource;
                await _repo.SaveResourceAsync(resource);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<InfraResource?> GetInfraResourceAsync(string id)
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