using Microsoft.Extensions.Diagnostics.HealthChecks;
using RedMaple.Orchestrator.Contracts.Deployments;
using RedMaple.Orchestrator.Contracts.Healthz;

namespace RedMaple.Orchestrator.Controller.Domain.Healthz
{
    public interface IHealthzChecker
    {
        Task<HealthStatus> CheckLivezAsync(
            ApplicationDeployment applicationDeployment,
            DeploymentPlan deployment, 
            CancellationToken cancellationToken);
        Task<HealthStatus> CheckReadyzAsync(
            ApplicationDeployment applicationDeployment,
            DeploymentPlan deployment, CancellationToken cancellationToken);

        Task<HealthStatus> CheckAsync(
            ApplicationDeployment deployment,
            DeploymentPlan deploymentPlan, 
            DeploymentHealthCheck check, 
            CancellationToken cancellationToken);
    }
}