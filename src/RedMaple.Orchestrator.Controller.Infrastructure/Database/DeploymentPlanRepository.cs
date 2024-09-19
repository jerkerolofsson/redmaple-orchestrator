using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.Contracts.Deployments;
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
    internal class DeploymentPlanRepository : DocumentStore<DeploymentPlan>, IDeploymentPlanRepository
    {
        private readonly ILogger _logger;

        public DeploymentPlanRepository(ILogger<DeploymentPlanRepository> logger)
        {
            _logger = logger;
        }
        protected override string SaveFilePath => GetConfigFilePath();

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
            var filePath = Path.Combine(path, "deployments.json");

            if (!File.Exists(filePath))
            {
                // Create empty
                File.WriteAllText(filePath, JsonSerializer.Serialize(new List<DeploymentPlan>()));
            }
            return filePath;
        }

        public async Task SaveDeploymentPlanAsync(DeploymentPlan service)
        {
            if (string.IsNullOrWhiteSpace(service.Id))
            {
                service.Id = Guid.NewGuid().ToString();
            }
            _logger.LogDebug("Saving deployment plan with ID {id}..", service.Id);
            var services = await LoadAsync();
            services.RemoveAll(x => x.Id == service.Id);
            services.Add(service);
            await CommitAsync(services);
        }
        public async Task AddDeploymentPlanAsync(DeploymentPlan service)
        {
            if (string.IsNullOrWhiteSpace(service.Id))
            {
                service.Id = Guid.NewGuid().ToString();
            }
            _logger.LogInformation("Adding deployment plan with ID {id}..", service.Id);
            var services = await LoadAsync();
            services.RemoveAll(x => x.Id == service.Id);
            services.Add(service);
            await CommitAsync(services);
        }

        public async Task<List<DeploymentPlan>> GetDeploymentPlansAsync()
        {
            return await LoadAsync();
        }

        public async Task DeleteDeploymentPlanAsync(string id)
        {
            _logger.LogInformation("Removing deployment plan with ID {id}..", id);
            var services = await LoadAsync();
            services.RemoveAll(x => x.Id == id);
            await CommitAsync(services);
        }


    }
}
