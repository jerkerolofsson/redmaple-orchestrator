using RedMaple.Orchestrator.Contracts.Deployments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Deployments
{
    public interface IDeploymentPlanRepository
    {
        Task<List<DeploymentPlan>> GetDeploymentPlansAsync();
        Task DeleteDeploymentPlanAsync(string id);
        Task AddDeploymentPlanAsync(DeploymentPlan service);
        Task SaveDeploymentPlanAsync(DeploymentPlan service);
    }
}
