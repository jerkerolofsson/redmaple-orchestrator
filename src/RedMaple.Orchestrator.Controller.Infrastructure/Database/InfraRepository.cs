using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.Contracts.Deployments;
using RedMaple.Orchestrator.Contracts.Healthz;
using RedMaple.Orchestrator.Contracts.Infra;
using RedMaple.Orchestrator.Infrastructure;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace RedMaple.Orchestrator.Controller.Infrastructure.Database
{
    internal class InfraRepository : DocumentStore<InfraResource>, IInfraRepository
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        protected override string SaveFilePath => GetConfigFilePath();

        private string GetLocalConfigDir()
        {
            string path = "/data/redmaple/controller/infra";
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (isWindows)
            {
                path = @"C:\temp\redmaple\controller\infra";
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
            var filePath = Path.Combine(path, "infra-resources.json");
            return filePath;
        }
        public async Task<List<InfraResource>> GetResourcesAsync()
        {
            return await LoadAsync();
        }

        public async Task SaveResourceAsync(InfraResource resource)
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

        public async Task DeleteResourceAsync(InfraResource resource)
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
