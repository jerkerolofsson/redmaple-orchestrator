using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.Contracts.Ingress;
using RedMaple.Orchestrator.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Infrastructure.Database
{
    internal class FileSystemIngressRepository : DocumentStore<IngressServiceDescription>, IIngressRepository
    {
        private readonly ILogger _logger;

        private SemaphoreSlim _semaphore = new SemaphoreSlim(1);


        protected override string SaveFilePath => GetConfigFilePath();

        public FileSystemIngressRepository(ILogger<FileSystemIngressRepository> logger)
        {
            _logger = logger;
        }

        private string GetLocalConfigDir()
        {
            string path = "/data/redmaple/controller/ingress";
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (isWindows)
            {
                path = @"C:\temp\redmaple\controller";
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
            var filePath = Path.Combine(path, "ingress.json");

            if (!File.Exists(filePath))
            {
                // Create empty
                File.WriteAllText(filePath, JsonSerializer.Serialize(new List<IngressServiceDescription>()));
            }
            return filePath;
        }

        public async Task AddIngressServiceAsync(IngressServiceDescription service)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (string.IsNullOrWhiteSpace(service.Id))
                {
                    service.Id = Guid.NewGuid().ToString();
                }
                _logger.LogInformation("Adding ingress service with ID {id}..", service.Id);
                var services = await LoadAsync();
                services.Add(service);
                await CommitAsync(services);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<List<IngressServiceDescription>> GetServicesAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                return await LoadAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task DeleteIngressServiceAsync(string id)
        {
            _logger.LogInformation("Removing ingress service with ID {id}..", id);
            await _semaphore.WaitAsync();
            try
            {
                var services = await LoadAsync();
                services.RemoveAll(x => x.Id == id);
                await CommitAsync(services);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
