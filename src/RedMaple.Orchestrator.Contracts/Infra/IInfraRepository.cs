using RedMaple.Orchestrator.Contracts.Deployments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Infra
{
    public interface IInfraRepository
    {
        Task DeleteResourceAsync(InfraResource resource);
        Task<List<InfraResource>> GetResourcesAsync();
        Task SaveResourceAsync(InfraResource resource);
    }
}
