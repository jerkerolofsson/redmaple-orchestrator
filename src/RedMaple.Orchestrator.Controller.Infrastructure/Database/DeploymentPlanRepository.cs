﻿using Microsoft.Extensions.Logging;
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
    internal class DeploymentPlanRepository : IDeploymentPlanRepository
    {
        private readonly ILogger _logger;

        public DeploymentPlanRepository(ILogger<DeploymentPlanRepository> logger)
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
            _logger.LogInformation("Saving deployment plan with ID {id}..", service.Id);
            var services = await LoadDataAsync();
            services.RemoveAll(x => x.Id == service.Id);
            services.Add(service);
            await WriteDataAsync(services);
        }
        public async Task AddDeploymentPlanAsync(DeploymentPlan service)
        {
            if (string.IsNullOrWhiteSpace(service.Id))
            {
                service.Id = Guid.NewGuid().ToString();
            }
            _logger.LogInformation("Adding deployment plan with ID {id}..", service.Id);
            var services = await LoadDataAsync();
            services.RemoveAll(x => x.Id == service.Id);
            services.Add(service);
            await WriteDataAsync(services);
        }

        public async Task<List<DeploymentPlan>> GetDeploymentPlansAsync()
        {
            return await LoadDataAsync();
        }

        public async Task DeleteDeploymentPlanAsync(string id)
        {
            _logger.LogInformation("Removing deployment plan with ID {id}..", id);
            var services = await LoadDataAsync();
            services.RemoveAll(x => x.Id == id);
            await WriteDataAsync(services);
        }

        private async Task<List<DeploymentPlan>> LoadDataAsync()
        {
            var path = GetConfigFilePath();
            try
            {
                _logger.LogInformation("Loading deployment plans from {path}", path);
                var json = await File.ReadAllTextAsync(path);
                var services = JsonSerializer.Deserialize<List<DeploymentPlan>>(json);
                return services ?? new();
            }
            catch (Exception) { }
            return new List<DeploymentPlan>();
        }

        private JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions() { WriteIndented = true };

        private async Task WriteDataAsync(List<DeploymentPlan> services)
        {
            var json = JsonSerializer.Serialize(services, _jsonSerializerOptions);
            var path = GetConfigFilePath();

            _logger.LogInformation("Writing deployment plans to {path}", path);
            await File.WriteAllTextAsync(path, json);
        }

    }
}
