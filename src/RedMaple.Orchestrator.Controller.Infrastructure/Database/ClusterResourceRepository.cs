using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.Contracts.Deployments;
using RedMaple.Orchestrator.Contracts.Healthz;
using RedMaple.Orchestrator.Contracts.Resources;
using RedMaple.Orchestrator.Infrastructure;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace RedMaple.Orchestrator.Controller.Infrastructure.Database
{
    internal class ClusterResourceRepository : DocumentStore<ClusterResource>, IClusterResourceRepository
    {
        private SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        protected override string SaveFilePath => GetConfigFilePath();

        private string GetLocalConfigDir()
        {
            string path = "/data/redmaple/controller/resources";
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (isWindows)
            {
                path = @"C:\temp\redmaple\controller\resources";
            }
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        private string GetConfigFilePath()
        {
            string path = GetLocalConfigDir();
            var filePath = Path.Combine(path, "persistant.json");
            return filePath;
        }
        public async Task<List<ClusterResource>> GetResourcesAsync()
        {
            return await LoadAsync();
        }

        public async Task SaveResourceAsync(ClusterResource resource)
        {
            await _semaphore.WaitAsync();
            try
            {
                var resources = await LoadAsync();
                resources.RemoveAll(x => x.Id == resource.Id);
                resources.Add(resource);
                await CommitAsync(resources);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task DeleteResourceAsync(ClusterResource resource)
        {
            await _semaphore.WaitAsync();
            try
            {
                var resources = await LoadAsync();
                resources.RemoveAll(x => x.Id == resource.Id);
                await CommitAsync(resources);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
