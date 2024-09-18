using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.Contracts.Ingress;
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
    internal class FileSystemIngressRepository : IIngressRepository
    {
        private readonly ILogger _logger;

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
            if (string.IsNullOrWhiteSpace(service.Id))
            {
                service.Id = Guid.NewGuid().ToString();
            }
            _logger.LogInformation("Adding ingress service with ID {id}..", service.Id);
            var services = await LoadDataAsync();
            services.Add(service);
            await WriteDataAsync(services);
        }

        public async Task<List<IngressServiceDescription>> GetServicesAsync()
        {
            return await LoadDataAsync();
        }

        public async Task DeleteIngressServiceAsync(string id)
        {
            _logger.LogInformation("Removing ingress service with ID {id}..", id);
            var services = await LoadDataAsync();
            services.RemoveAll(x => x.Id == id);
            await WriteDataAsync(services);
        }

        private async Task<List<IngressServiceDescription>> LoadDataAsync()
        {
            var path = GetConfigFilePath();
            try
            {
                _logger.LogDebug("Loading service configuration from {path}", path);
                var json = await File.ReadAllTextAsync(path);
                var services = JsonSerializer.Deserialize<List<IngressServiceDescription>>(json);
                return services ?? new();
            }
            catch (Exception) { }
            return new List<IngressServiceDescription>();
        }
        private async Task WriteDataAsync(List<IngressServiceDescription> services)
        {
            var json = JsonSerializer.Serialize(services);
            var path = GetConfigFilePath();

            _logger.LogDebug("Writing service configuration to {path}", path);
            await File.WriteAllTextAsync(path, json);
        }

    }
}
