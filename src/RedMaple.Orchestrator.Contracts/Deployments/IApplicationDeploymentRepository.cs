using RedMaple.Orchestrator.Contracts.Deployments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Deployments
{
    public interface IApplicationDeploymentRepository
    {
        Task<List<Deployment>> GetDeploymentsAsync();
        Task DeleteDeploymentAsync(string id);
        Task AddDeploymentAsync(Deployment deployment);
        Task SaveDeploymentAsync(Deployment deployment);
    }
}
