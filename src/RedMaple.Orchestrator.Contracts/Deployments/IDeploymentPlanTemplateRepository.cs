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
        Task<List<DeploymentPlanTemplate>> GetDeploymentPlansAsync();
        Task DeleteDeploymentPlanAsync(string name);
        Task AddDeploymentPlanAsync(DeploymentPlanTemplate service);
    }
}
