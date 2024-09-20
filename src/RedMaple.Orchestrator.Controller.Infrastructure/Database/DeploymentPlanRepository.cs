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
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);

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
            _logger.LogTrace("Saving deployment plan with ID {id}..", service.Id);

            await _lock.WaitAsync();
            try
            {
                var services = await LoadAsync();
                services.RemoveAll(x => x.Id == service.Id);
                services.Add(service);
                await CommitAsync(services);
            }
            finally
            {
                _lock.Release();
            }
        }
        public async Task AddDeploymentPlanAsync(DeploymentPlan service)
        {
            if (string.IsNullOrWhiteSpace(service.Id))
            {
                service.Id = Guid.NewGuid().ToString();
            }
            _logger.LogTrace("Adding deployment plan with ID {id}..", service.Id);
            await _lock.WaitAsync();
            try
            {
                var services = await LoadAsync();
                services.RemoveAll(x => x.Id == service.Id);
                services.Add(service);
                await CommitAsync(services);
            }
            finally
            {
                _lock.Release();

                }
        }

        public async Task<List<DeploymentPlan>> GetDeploymentPlansAsync()
        {
            await _lock.WaitAsync();
            try
            {
                return await LoadAsync();
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task DeleteDeploymentPlanAsync(string id)
        {
            await _lock.WaitAsync();
            try
            {
                await DeleteDeploymentPlanNoLockAsync(id);
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task DeleteDeploymentPlanNoLockAsync(string id)
        {
            _logger.LogInformation("Removing deployment plan with ID {id}..", id);
            var services = await LoadAsync();
            services.RemoveAll(x => x.Id == id);
            await CommitAsync(services);
        }
    }
}
