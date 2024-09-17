using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Domain.Deployments.State
{
    /// <summary>
    /// Raises events for existing deployments when the application is starting
    /// </summary>
    public class DeploymentStartupService : BackgroundService, IProgress<string>
    {
        private readonly IDeploymentManager _deploymentManager;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public DeploymentStartupService(
            ILogger<DeploymentStartupService> logger,
            IDeploymentManager deploymentManager,
            IMediator ediator)
        {
            _logger = logger;
            _deploymentManager = deploymentManager;
            _mediator = ediator;
        }

        public void Report(string value)
        {
            _logger.LogInformation("DeploymentStartupService: " + value);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach (var plan in await _deploymentManager.GetDeploymentPlansAsync())
            {
                _logger.LogInformation("DeploymentStartupService: Initializing deployment {DeploymentSlug}..", plan.Slug);
                foreach (var deployment in await _deploymentManager.GetApplicationDeploymentsAsync(plan.Slug))
                {
                    try
                    {
                        await _mediator.Publish(new AppDeploymentStartingNotification(deployment));

                        await _deploymentManager.WaitUntilReadyzAsync(plan, deployment, this, stoppingToken);

                        await _mediator.Publish(new AppDeploymentReadyNotification(deployment));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during DeploymentStartupService");
                    }
                }
            }
        }
    }
}
