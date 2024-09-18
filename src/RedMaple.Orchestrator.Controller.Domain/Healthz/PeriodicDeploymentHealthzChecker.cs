using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.Contracts.Deployments;
using RedMaple.Orchestrator.Contracts.Healthz;
using RedMaple.Orchestrator.Controller.Domain.Deployments;
using RedMaple.Orchestrator.Controller.Domain.Healthz.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Domain.Healthz
{
    public class PeriodicDeploymentHealthzChecker : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IHealthzChecker _healthChecker;
        private readonly IDeploymentManager _deploymentManager;

        private Dictionary<string, DateTime> _lastChecks = new();

        public PeriodicDeploymentHealthzChecker(
            ILogger<PeriodicDeploymentHealthzChecker> logger,
            IHealthzChecker healthChecker, 
            IDeploymentManager deploymentManager)
        {
            _logger = logger;
            _healthChecker = healthChecker;
            _deploymentManager = deploymentManager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                TimeSpan sleepTime = TimeSpan.FromSeconds(5);

                try
                {
                    var deployments = await _deploymentManager.GetDeploymentPlansAsync();
                    sleepTime = await RunTestsAsync(sleepTime, deployments, stoppingToken);
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "Failed to run tests");
                }

                if (sleepTime < TimeSpan.FromSeconds(1))
                {
                    sleepTime = TimeSpan.FromSeconds(1);
                }
                await Task.Delay(sleepTime, stoppingToken);
            }
        }

        private async Task<TimeSpan> RunTestsAsync(TimeSpan sleepTime, List<DeploymentPlan> deployments, CancellationToken stoppingToken)
        {
            foreach (var deployment in deployments)
            {
                var deploymentHealth = new ResourceHealthCheckResult { Status = HealthStatus.Healthy };
                for (int i = 0; i < deployment.HealthChecks.Count; i++)
                {
                    var check = deployment.HealthChecks[i];
                    if (check.Type != HealthCheckType.Livez)
                    {
                        continue;
                    }

                    var checkKey = deployment.Slug + i;
                    TimeSpan timeSince = GetTimeSinceLastCheck(checkKey);
                    if (NeedCheck(check, timeSince))
                    {
                        var startTimestamp = Stopwatch.GetTimestamp();
                        HealthStatus status = await CheckHealthAsync(deployment, check, stoppingToken);
                        deploymentHealth.Status = status;
                        deploymentHealth.Duration = Stopwatch.GetElapsedTime(startTimestamp);
                    }
                    else
                    {
                        TimeSpan checkInterval = TimeSpan.FromSeconds(check.Interval);
                        TimeSpan timeUntilCheck = checkInterval - timeSince;
                        if (timeUntilCheck < sleepTime)
                        {
                            sleepTime = timeUntilCheck;
                        }
                    }
                }

                await UpdateHealthStatusAsync(deployment, deploymentHealth);
            }

            return sleepTime;
        }

        private async Task UpdateHealthStatusAsync(DeploymentPlan deployment, ResourceHealthCheckResult deploymentHealth)
        {
            if (deploymentHealth.Status == HealthStatus.Healthy)
            {
                _logger.LogDebug("{DeploymentSlug} health changed to {HealthStatus}", deployment.Slug, deploymentHealth.Status);
            }
            else
            {
                _logger.LogWarning("{DeploymentSlug} health changed to {HealthStatus}", deployment.Slug, deploymentHealth.Status);
            }
            deployment.Health = deploymentHealth;
            await _deploymentManager.SaveAsync(deployment);
        }

        private async Task<HealthStatus> CheckHealthAsync(
            DeploymentPlan deploymentPlan, 
            DeploymentHealthCheck check, 
            CancellationToken stoppingToken)
        {
            var status = HealthStatus.Healthy;
            var timeoutInSeconds = check.Timeout;
            if (timeoutInSeconds <= 0)
            {
                timeoutInSeconds = 5;
            }
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(check.Timeout));
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeout.Token);
            try
            {

                _logger.LogDebug("{DeploymentSlug}: Testing health (livez)..", deploymentPlan.Slug);

                var deployments = await _deploymentManager.GetApplicationDeploymentsAsync(deploymentPlan.Slug);

                foreach (var appDeployment in deployments)
                {
                    status = await _healthChecker.CheckAsync(appDeployment, deploymentPlan, check, cts.Token);
                }
                _logger.LogDebug("{DeploymentSlug}: Health (livez)={HealthStatus}", deploymentPlan.Slug, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception thrown when performing health check");
                status = HealthStatus.Unhealthy;
            }
            return status;
        }

        private static bool NeedCheck(DeploymentHealthCheck check, TimeSpan timeSince)
        {
            TimeSpan checkInterval = TimeSpan.FromSeconds(check.Interval);
            bool shouldCheck = false;
            if (checkInterval > TimeSpan.Zero)
            {
                if (timeSince > checkInterval)
                {
                    shouldCheck = true;
                }
            }
            return shouldCheck;
        }

        private TimeSpan GetTimeSinceLastCheck(string checkKey)
        {
            TimeSpan timeSince = TimeSpan.MaxValue;
            if (_lastChecks.TryGetValue(checkKey, out var lastCheck))
            {
                timeSince = DateTime.Now - lastCheck;
            }

            return timeSince;
        }
    }
}
