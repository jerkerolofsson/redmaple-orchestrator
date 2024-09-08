using RedMaple.Orchestrator.Contracts.Ingress;
using RedMaple.Orchestrator.Contracts.Node;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace RedMaple.Orchestrator.Ingress
{
    public class LocalIngressServiceController : IIngressServiceController
    {
        private readonly IReverseProxy _reverseProxy;

        public LocalIngressServiceController(IReverseProxy reverseProxy)
        {
            _reverseProxy = reverseProxy;
        }

        private string GetLocalConfigDir()
        {
            string path = "/data/redmaple/node";
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (isWindows)
            {
                path = @"C:\temp\redmaple\node";
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


        private async Task WriteSettingsAsync(List<IngressServiceDescription> services)
        {
            var json = JsonSerializer.Serialize(services);
            var path = GetConfigFilePath();
            await File.WriteAllTextAsync(path, json);
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

        public async Task AddIngressServiceAsync(IngressServiceDescription service)
        {
            var services = await LoadSettingsAsync();
            services.Add(service);
            await WriteSettingsAsync(services);

            await _reverseProxy.UpdateConfigurationAsync(services);
            await _reverseProxy.StartAsync();
        }

        public async Task DeleteIngressServiceAsync(string id)
        {
            var services = await LoadSettingsAsync();
            services.RemoveAll(x => x.Id == id);
            await WriteSettingsAsync(services);

            await _reverseProxy.UpdateConfigurationAsync(services);
            await _reverseProxy.StartAsync();
        }

        public async Task<List<IngressServiceDescription>> GetServicesAsync()
        {
            return await LoadSettingsAsync();
        }
    }
}