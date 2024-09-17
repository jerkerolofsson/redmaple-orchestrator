using Docker.DotNet.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RedMaple.Orchestrator.Contracts.Healthz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Domain.Healthz
{
    /// <summary>
    /// Checks for health
    /// </summary>
    internal class TcpConnectHealthzChecker : IHealthzCheckerMethod
    {
        static TcpConnectHealthzChecker()
        {
        }

        public async Task<HealthStatus> CheckAsync(DeploymentHealthCheck check, string url, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(url);

            try
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    using var client = new TcpClient();
                    await client.ConnectAsync(uri.Host, uri.Port);
                    return HealthStatus.Healthy;
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
