using System.Diagnostics;

using MediatR;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using RedMaple.Orchestrator.Contracts.Deployments;
using RedMaple.Orchestrator.Contracts.Healthz;
using RedMaple.Orchestrator.Controller.Domain.Deployments;
using RedMaple.Orchestrator.Controller.Domain.Healthz.Models;
using RedMaple.Orchestrator.Controller.Domain.Node;

namespace RedMaple.Orchestrator.Controller.Domain.Healthz
{
    public class PeriodicDeploymentHealthzChecker : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IHealthzChecker _healthChecker;
        private readonly IDeploymentManager _deploymentManager;
        private readonly INodeManager _nodeManager;
        private readonly IMediator _mediator;

        private readonly Dictionary<string, DateTime> _lastChecks = new();

        public PeriodicDeploymentHealthzChecker(
            ILogger<PeriodicDeploymentHealthzChecker> logger,
            IHealthzChecker healthChecker,
            IDeploymentManager deploymentManager,
            INodeManager nodeManager,
            IMediator mediator)
        {
            _logger = logger;
            _healthChecker = healthChecker;
            _deploymentManager = deploymentManager;
            _nodeManager = nodeManager;
            _mediator = mediator;
        }

        private TimeSpan _sleepTime = TimeSpan.FromSeconds(60);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await InitHealthStatusAsync();

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                _sleepTime = TimeSpan.FromSeconds(60);

                try
                {
                    var deployments = await _deploymentManager.GetDeploymentPlansAsync();
                    await RunTestsAsync(deployments, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to run tests");
                }

                if (_sleepTime < TimeSpan.FromSeconds(5))
                {
                    _sleepTime = TimeSpan.FromSeconds(5);
                }
                await Task.Delay(_sleepTime, stoppingToken);
            }
        }

        private async Task InitHealthStatusAsync()
        {
            var deployments = await _deploymentManager.GetDeploymentPlansAsync();
            foreach (var deployment in deployments)
            {
                deployment.Health = null;
                deployment.HealthStatusChangedTimestamp = null;
            }
        }

        private async Task<HealthStatus> CheckApplicationServerHealthAsync(DeploymentPlan deploymentPlan)
        {
            if (deploymentPlan.ApplicationServerIps is not null)
            {
                foreach (var appServerIp in deploymentPlan.ApplicationServerIps)
                {
                    var status = await GetNodeHealthAsync(appServerIp);
                    if (status == HealthStatus.Unhealthy)
                    {
                        _logger.LogWarning("{DeploymentSlug}: App server ({ServerIp}) is offline..", deploymentPlan.Slug, appServerIp);
                        return status;
                    }
                }
            }
            return HealthStatus.Healthy;
        }
        private async Task<HealthStatus> CheckIngressHealthAsync(DeploymentPlan deploymentPlan)
        {
            if (deploymentPlan.IngressServerIp is not null)
            {
                var status = await GetNodeHealthAsync(deploymentPlan.IngressServerIp);
                if (status == HealthStatus.Unhealthy)
                {
                    _logger.LogWarning("{DeploymentSlug}: Ingress server ({ServerIp}) is offline..", deploymentPlan.Slug, deploymentPlan.IngressServerIp);
                    return status;
                }
            }
            return HealthStatus.Healthy;
        }

        private async Task RunTestsAsync(List<DeploymentPlan> deployments, CancellationToken stoppingToken)
        {
            foreach (var deployment in deployments)
            {
                var prevHealth = deployment.Health;
                var deploymentHealth = new ResourceHealthCheckResult { Status = HealthStatus.Healthy };

                // Always check ingress and application server node status
                var ingressStatus = await CheckIngressHealthAsync(deployment);
                var appServerStatus = await CheckApplicationServerHealthAsync(deployment);
                if (ingressStatus == HealthStatus.Unhealthy)
                {
                    await UpdateHealthStatusAsync(deployment, prevHealth, new ResourceHealthCheckResult
                    {
                        Status = HealthStatus.Unhealthy,
                    });
                }
                else if (appServerStatus == HealthStatus.Unhealthy)
                {
                    await UpdateHealthStatusAsync(deployment, prevHealth, new ResourceHealthCheckResult
                    {
                        Status = HealthStatus.Unhealthy,
                    });
                }
                else
                {
                    var didCheck = await CheckHealthStatusAsync(deployment, deploymentHealth, stoppingToken);
                    if (didCheck)
                    {
                        if(deployment.Health?.Status == HealthStatus.Unhealthy)
                        {
                            _logger.LogWarning("{DeploymentSlug}: Health test failed..", deployment.Slug);
                        }

                        await UpdateHealthStatusAsync(deployment, prevHealth, deploymentHealth);
                    }
                }
            }
        }


        private async Task<bool> CheckHealthStatusAsync(DeploymentPlan deployment, ResourceHealthCheckResult deploymentHealth, CancellationToken stoppingToken)
        {
            bool didCheck = false;

            // Default to healthy if there are no checks
            if (deployment.HealthChecks.Count == 0)
            {
                deploymentHealth.Status = HealthStatus.Healthy;
                deploymentHealth.Duration = TimeSpan.Zero;
                didCheck = true;
            }

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
                    didCheck = true;
                }
                else
                {
                    TimeSpan checkInterval = TimeSpan.FromSeconds(check.Interval);
                    TimeSpan timeUntilCheck = checkInterval - timeSince;
                    if (timeUntilCheck < _sleepTime)
                    {
                        _sleepTime = timeUntilCheck;
                    }
                }
            }
            return didCheck;
        }

        private async Task UpdateHealthStatusAsync(DeploymentPlan deployment, ResourceHealthCheckResult? prevHealth, ResourceHealthCheckResult newHealth)
        {
            if(deployment.Health is null)
            {
                deployment.HealthStatusChangedTimestamp = DateTime.UtcNow;
                _logger.LogInformation("{DeploymentSlug} health is {HealthStatus}", deployment.Slug, newHealth.Status);
            }
            else if (deployment.Health.Status != newHealth.Status)
            {
                // Health status has changed
                if(newHealth.Status != HealthStatus.Healthy)
                {
                    _logger.LogWarning("{DeploymentSlug} health is {HealthStatus}", deployment.Slug, newHealth.Status);
                }
                deployment.HealthStatusChangedTimestamp = DateTime.UtcNow;
            }

            deployment.Health = newHealth;
            await _deploymentManager.SaveAsync(deployment);

            if(newHealth.Status == HealthStatus.Unhealthy)
            {
                try
                {
                    _logger.LogWarning("{DeploymentSlug} publishing AppDeploymentUnhealthyNotification", deployment.Slug);
                    await _mediator.Publish(new AppDeploymentUnhealthyNotification(deployment));
                } 
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception when publishing AppDeploymentUnhealthyNotification");
                }
            }
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
                _logger.LogTrace("{DeploymentSlug}: Testing health (livez)..", deploymentPlan.Slug);

                var deployments = await _deploymentManager.GetApplicationDeploymentsAsync(deploymentPlan.Slug);
                foreach (var appDeployment in deployments)
                {
                    status = await _healthChecker.CheckAsync(appDeployment, deploymentPlan, check, cts.Token);
                    if (status == HealthStatus.Unhealthy)
                    {
                        _logger.LogWarning("{DeploymentSlug}: Deployment on {AppServerIp} is unhealthy..", deploymentPlan.Slug, appDeployment.ApplicationServerIp);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception thrown when performing health check for {deploymentPlan.Slug}");
                status = HealthStatus.Unhealthy;
            }
            return status;
        }

        private async Task<HealthStatus> GetNodeHealthAsync(string serverIp)
        {
            var node = await _nodeManager.GetNodeByIpAddressAsync(serverIp);
            if(node is null)
            {
                return HealthStatus.Unhealthy;
            }
            return HealthStatus.Healthy;
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
