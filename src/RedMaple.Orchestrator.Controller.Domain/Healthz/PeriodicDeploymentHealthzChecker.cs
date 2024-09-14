using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.Contracts.Deployments;
using RedMaple.Orchestrator.Contracts.Healthz;
using RedMaple.Orchestrator.Controller.Domain.Deployments;
using System;
using System.Collections.Generic;
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

                var deployments = await _deploymentManager.GetDeploymentPlansAsync();
                foreach (var deployment in deployments)
                {
                    var deploymentHealthStatus = HealthStatus.Healthy;
                    for (int i=0;i<deployment.HealthChecks.Count; i++)
                    {
                        var check = deployment.HealthChecks[i];
                        if(check.Type != HealthCheckType.Livez)
                        {
                            continue;
                        }

                        var checkKey = deployment.Slug + i;
                        TimeSpan timeSince = GetTimeSinceLastCheck(checkKey);
                        if(NeedCheck(check, timeSince))
                        {
                            HealthStatus status = await CheckHealthAsync(deployment, check, stoppingToken);
                            deploymentHealthStatus = status;
                        }
                        else
                        {
                            TimeSpan checkInterval = TimeSpan.FromSeconds(check.Interval);
                            TimeSpan timeUntilCheck = checkInterval - timeSince;
                            if(timeUntilCheck < sleepTime)
                            {
                                sleepTime = timeUntilCheck;
                            }
                        }
                    }

                    if(deploymentHealthStatus != deployment.HealthStatus)
                    {
                        await UpdateHealthStatusAsync(deployment, deploymentHealthStatus);
                    }
                }

                if (sleepTime < TimeSpan.FromSeconds(1))
                {
                    sleepTime = TimeSpan.FromSeconds(1);
                }
                await Task.Delay(sleepTime, stoppingToken);
            }
        }

        private async Task UpdateHealthStatusAsync(DeploymentPlan deployment, HealthStatus deploymentHealthStatus)
        {
            if (deploymentHealthStatus == HealthStatus.Healthy)
            {
                _logger.LogInformation("{DeploymentSlug} is health changed to {HealthStatus}", deployment.Slug, deploymentHealthStatus);
            }
            else
            {
                _logger.LogWarning("{DeploymentSlug} is health changed to {HealthStatus}", deployment.Slug, deploymentHealthStatus);
            }
            deployment.HealthStatus = deploymentHealthStatus;
            await _deploymentManager.SaveAsync(deployment);
        }

        private async Task<HealthStatus> CheckHealthAsync(DeploymentPlan deployment, DeploymentHealthCheck check, CancellationToken stoppingToken)
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
                _logger.LogDebug("{DeploymentSlug}: Testing health (livez)..", deployment.Slug);
                status = await _healthChecker.CheckAsync(deployment, check, cts.Token);
                _logger.LogDebug("{DeploymentSlug}: Health (livez)={HealthStatus}", deployment.Slug, status);
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
