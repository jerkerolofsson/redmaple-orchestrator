using Microsoft.Extensions.Diagnostics.HealthChecks;
using RedMaple.Orchestrator.Contracts.Deployments;
using RedMaple.Orchestrator.Contracts.Healthz;

namespace RedMaple.Orchestrator.Controller.Domain.Healthz
{
    public interface IHealthzChecker
    {
        Task<HealthStatus> CheckLivezAsync(DeploymentPlan deployment, CancellationToken cancellationToken);
        Task<HealthStatus> CheckReadyzAsync(DeploymentPlan deployment, CancellationToken cancellationToken);

        Task<HealthStatus> CheckAsync(DeploymentPlan deployment, DeploymentHealthCheck check, CancellationToken cancellationToken);
        Task<HealthStatus> CheckAsync(DeploymentPlan deployment, HealthCheckType type, CancellationToken cancellationToken);
    }
}