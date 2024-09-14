using RedMaple.Orchestrator.Contracts.Deployments;

namespace RedMaple.Orchestrator.Controller.Domain.Deployments.Templates
{
    public interface IDeploymentTemplateProvider
    {
        Task<List<DeploymentPlanTemplate>> GetDeploymentsAsync();
    }
}