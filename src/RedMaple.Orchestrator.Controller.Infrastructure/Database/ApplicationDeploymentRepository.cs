using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.Contracts.Deployments;
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
    internal class ApplicationDeploymentRepository : IApplicationDeploymentRepository
    {
        private readonly ILogger _logger;

        public ApplicationDeploymentRepository(
            ILogger<ApplicationDeploymentRepository> logger)
        {
            _logger = logger;
        }

        private string GetLocalConfigDir()
        {
            string path = "/data/redmaple/controller/deployments";
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
            var filePath = Path.Combine(path, "current.json");

            if (!File.Exists(filePath))
            {
                // Create empty
                File.WriteAllText(filePath, JsonSerializer.Serialize(new List<ApplicationDeployment>()));
            }
            return filePath;
        }

        public async Task SaveDeploymentAsync(ApplicationDeployment service)
        {
            _logger.LogDebug("Saving deployment plan with ID {id}..", service.Id);
            var services = await LoadDataAsync();
            services.RemoveAll(x => x.Id == service.Id);
            services.Add(service);
            await WriteDataAsync(services);
        }
        public async Task AddDeploymentAsync(ApplicationDeployment service)
        {
            _logger.LogInformation("Adding deployment plan with ID {id}..", service.Id);
            var services = await LoadDataAsync();
            services.RemoveAll(x => x.Id == service.Id);
            services.Add(service);
            await WriteDataAsync(services);
        }

        public async Task<List<ApplicationDeployment>> GetDeploymentsAsync()
        {
            return await LoadDataAsync();
        }

        public async Task DeleteDeploymentAsync(string id)
        {
            _logger.LogInformation("Removing deployment with ID {id}..", id);
            var services = await LoadDataAsync();
            services.RemoveAll(x => x.Id == id);
            await WriteDataAsync(services);
        }

        private async Task<List<ApplicationDeployment>> LoadDataAsync()
        {
            var path = GetConfigFilePath();
            try
            {
                _logger.LogDebug("Loading deployment from {path}", path);
                var json = await File.ReadAllTextAsync(path);
                var services = JsonSerializer.Deserialize<List<ApplicationDeployment>>(json);
                return services ?? new();
            }
            catch (Exception) { }
            return new List<ApplicationDeployment>();
        }

        private JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions() { WriteIndented = true };

        private async Task WriteDataAsync(List<ApplicationDeployment> services)
        {
            var json = JsonSerializer.Serialize(services, _jsonSerializerOptions);
            var path = GetConfigFilePath();

            _logger.LogDebug("Writing deployments to {path}", path);
            await File.WriteAllTextAsync(path, json);
        }

    }
}
