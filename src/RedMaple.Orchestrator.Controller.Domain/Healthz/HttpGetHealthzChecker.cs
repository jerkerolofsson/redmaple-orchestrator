using Docker.DotNet.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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
    internal class HttpGetHealthzChecker : IHealthzCheckerMethod
    {
        private static readonly HttpClient _httpClient;

        static HttpGetHealthzChecker()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
            _httpClient = new HttpClient(handler);
        }

        public async Task<HealthStatus> CheckAsync(DeploymentHealthCheck check, string url, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(url);

            try
            {
                using var response = await _httpClient.GetAsync(url, cancellationToken);
                int statusCode = (int)response.StatusCode;
                if (statusCode == check.HealthyStatusCode)
                {
                    return HealthStatus.Healthy;
                }
                if (statusCode == check.DegradedStatusCode)
                {
                    return HealthStatus.Degraded;
                }
                return HealthStatus.Unhealthy;
            }
            catch(Exception)
            {
                return HealthStatus.Unhealthy;
            }
        }
    }
}
