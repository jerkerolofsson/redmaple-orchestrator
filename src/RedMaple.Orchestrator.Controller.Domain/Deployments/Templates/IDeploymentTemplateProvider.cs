using RedMaple.Orchestrator.Contracts.Deployments;

namespace RedMaple.Orchestrator.Controller.Domain.Deployments.Templates
{
    public interface IDeploymentTemplateProvider
    {
        Task<DeploymentPlanTemplate?> GetTemplateByNameAsync(string name);
        Task SaveTemplateAsync(DeploymentPlanTemplate template);
        Task DeleteTemplateAsync(string name);
        Task<List<DeploymentPlanTemplate>> GetTemplatesAsync();
    }
}