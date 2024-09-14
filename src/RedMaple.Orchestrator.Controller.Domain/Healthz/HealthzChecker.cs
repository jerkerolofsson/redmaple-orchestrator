using Microsoft.Extensions.Diagnostics.HealthChecks;
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
        public Task<HealthStatus> CheckReadyzAsync(DeploymentPlan deployment, CancellationToken cancellationToken) => 
            CheckAsync(deployment, HealthCheckType.Readyz, cancellationToken);
        public Task<HealthStatus> CheckLivezAsync(DeploymentPlan deployment, CancellationToken cancellationToken) => 
            CheckAsync(deployment, HealthCheckType.Livez, cancellationToken);

        /// <summary>
        /// Checks health with checks of a specific kind
        /// </summary>
        /// <param name="deployment"></param>
        /// <param name="type"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<HealthStatus> CheckAsync(
            DeploymentPlan deployment, 
            HealthCheckType type, 
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(deployment?.HealthChecks);

            var checksOfType = deployment.HealthChecks.Where(x => x.Type == type);

            HealthStatus healthStatus = HealthStatus.Healthy;
            foreach (var check in checksOfType)
            {
                var checkStatus = await CheckAsync(deployment, check, cancellationToken);
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
            DeploymentPlan deployment, 
            DeploymentHealthCheck check, 
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(check?.RelativeUrl);

            string absoluteUrl = GetHealthzEndPoint(deployment, check);

            IHealthzCheckerMethod checker = check.Method switch
            {
                HealthCheckMethod.HttpGet => new HttpGetHealthzChecker(),
                _ => throw new NotImplementedException()
            };

            var healthStatus = await checker.CheckAsync(check, absoluteUrl, cancellationToken);
            return healthStatus;
        }

        private static string GetHealthzEndPoint(DeploymentPlan deployment, DeploymentHealthCheck check)
        {
            string baseUrl = check.Target switch
            {
                HealthCheckTarget.Ingress => $"https://{deployment.IngressServerIp}",
                HealthCheckTarget.Application => $"{deployment.ApplicationProtocol}://{deployment.ApplicationServerIp}:{deployment.ApplicationServerPort}",
                _ => throw new NotImplementedException()
            };
            string absoluteUrl = baseUrl.TrimEnd('/') + '/' + check.RelativeUrl;
            return absoluteUrl;
        }
    }
}
