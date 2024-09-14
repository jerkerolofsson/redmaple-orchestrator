using Microsoft.Extensions.Diagnostics.HealthChecks;
using RedMaple.Orchestrator.Contracts.Healthz;

namespace RedMaple.Orchestrator.Controller.Domain.Healthz
{
    internal interface IHealthzCheckerMethod
    {
        Task<HealthStatus> CheckAsync(DeploymentHealthCheck check, string url, CancellationToken cancellationToken);
    }
}