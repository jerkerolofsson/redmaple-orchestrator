using Microsoft.Extensions.Diagnostics.HealthChecks;
using RedMaple.Orchestrator.Contracts.Deployments;
using RedMaple.Orchestrator.Contracts.Healthz;

namespace RedMaple.Orchestrator.Controller.Domain.Healthz
{
    public interface IHealthzChecker
    {
        Task<HealthStatus> CheckLivezAsync(
            Deployment applicationDeployment,
            DeploymentPlan deployment, 
            CancellationToken cancellationToken);
        Task<HealthStatus> CheckReadyzAsync(
            Deployment applicationDeployment,
            DeploymentPlan deployment, CancellationToken cancellationToken);

        Task<HealthStatus> CheckAsync(
            Deployment deployment,
            DeploymentPlan deploymentPlan, 
            DeploymentHealthCheck check, 
            CancellationToken cancellationToken);
    }
}