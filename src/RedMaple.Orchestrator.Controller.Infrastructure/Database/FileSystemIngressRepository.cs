using RedMaple.Orchestrator.Contracts.Ingress;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Infrastructure.Database
{
    internal class FileSystemIngressRepository : IIngressRepository
    {
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

            var services = await LoadSettingsAsync();
            services.Add(service);
            await WriteSettingsAsync(services);
        }

        public async Task<List<IngressServiceDescription>> GetServicesAsync()
        {
            return await LoadSettingsAsync();
        }

        public async Task DeleteIngressServiceAsync(string id)
        {
            var services = await LoadSettingsAsync();
            services.RemoveAll(x => x.Id == id);
            await WriteSettingsAsync(services);
        }
        public async Task<List<IngressServiceDescription>> LoadSettingsAsync()
        {
            var path = GetConfigFilePath();
            try
            {
                var json = await File.ReadAllTextAsync(path);
                var services = JsonSerializer.Deserialize<List<IngressServiceDescription>>(json);
                return services ?? new();
            }
            catch (Exception) { }
            return new List<IngressServiceDescription>();
        }
        private async Task WriteSettingsAsync(List<IngressServiceDescription> services)
        {
            var json = JsonSerializer.Serialize(services);
            var path = GetConfigFilePath();
            await File.WriteAllTextAsync(path, json);
        }

    }
}
