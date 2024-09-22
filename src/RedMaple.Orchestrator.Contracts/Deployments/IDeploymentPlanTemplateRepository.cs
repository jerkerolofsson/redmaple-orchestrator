using RedMaple.Orchestrator.Contracts.Deployments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Deployments
{
    public interface IDeploymentPlanTemplateRepository
    {
        Task<List<DeploymentPlanTemplate>> GetTemplatesAsync();
        Task DeleteTemplateAsync(string name);
        Task AddTemplateAsync(DeploymentPlanTemplate service);
        Task SaveTemplateAsync(DeploymentPlanTemplate service);
        Task<DeploymentPlanTemplate?> GetTemplateByNameAsync(string name);
    }
}
