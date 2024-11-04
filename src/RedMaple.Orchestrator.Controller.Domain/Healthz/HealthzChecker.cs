using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

using RedMaple.Orchestrator.Contracts.Deployments;
using RedMaple.Orchestrator.Contracts.Healthz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Domain.Healthz
{
    /// <summary>
    /// Checks for health
    /// </summary>
    internal class HealthzChecker : IHealthzChecker
    {
        private readonly ILogger<HealthzChecker> _logger;

        public Task<HealthStatus> CheckReadyzAsync(Deployment applicationDeployment, DeploymentPlan deployment, CancellationToken cancellationToken) => 
            CheckAsync(applicationDeployment, deployment, HealthCheckType.Readyz, cancellationToken);
        public Task<HealthStatus> CheckLivezAsync(Deployment applicationDeployment, DeploymentPlan deployment, CancellationToken cancellationToken) => 
            CheckAsync(applicationDeployment,deployment, HealthCheckType.Livez, cancellationToken);

        public HealthzChecker(ILogger<HealthzChecker> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Checks health with checks of a specific kind
        /// </summary>
        /// <param name="deployment">Information about the actual deployment</param>
        /// <param name="deploymentPlan">Plan, including health checks</param>
        /// <param name="type"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<HealthStatus> CheckAsync(
            Deployment deployment,
            DeploymentPlan deploymentPlan, 
            HealthCheckType type, 
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(deploymentPlan?.HealthChecks);

            var checksOfType = deploymentPlan.HealthChecks.Where(x => x.Type == type);

            HealthStatus healthStatus = HealthStatus.Healthy;
            foreach (var check in checksOfType)
            {
                var checkStatus = await CheckAsync(deployment, deploymentPlan, check, cancellationToken);
                if (checkStatus == HealthStatus.Unhealthy)
                {
                    healthStatus = HealthStatus.Unhealthy;
                    break;
                }
                else if (checkStatus == HealthStatus.Degraded)
                {
                    healthStatus = HealthStatus.Degraded;
                }
            }

            return healthStatus;
        }


        public async Task<HealthStatus> CheckAsync(
            Deployment deployment, 
            DeploymentPlan deploymentPlan,
            DeploymentHealthCheck check, 
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(check?.RelativeUrl);

            string absoluteUrl = GetHealthzEndPoint(deployment, deploymentPlan, check);

            IHealthzCheckerMethod checker = check.Method switch
            {
                HealthCheckMethod.HttpGet => new HttpGetHealthzChecker(_logger),
                HealthCheckMethod.TcpConnect => new TcpConnectHealthzChecker(_logger),
                _ => throw new NotImplementedException()
            };

            var healthStatus = await checker.CheckAsync(check, absoluteUrl, cancellationToken);
            return healthStatus;
        }

        private static string GetHealthzEndPoint(Deployment applicationDeployment, DeploymentPlan deployment, DeploymentHealthCheck check)
        {
            string baseUrl = check.Target switch
            {
                HealthCheckTarget.Ingress => $"https://{deployment.IngressServerIp}:443",
                HealthCheckTarget.Application => $"{deployment.ApplicationProtocol}://{applicationDeployment.ApplicationServerIp}:{applicationDeployment.ApplicationServerPort}",
                _ => throw new NotImplementedException()
            };
            string absoluteUrl = baseUrl.TrimEnd('/') + '/' + check.RelativeUrl;
            return absoluteUrl;
        }
    }
}
