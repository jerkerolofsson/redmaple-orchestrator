using RedMaple.Orchestrator.Contracts.Deployments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Resources
{
    public interface IClusterResourceRepository
    {
        Task DeleteResourceAsync(ClusterResource resource);
        Task<List<ClusterResource>> GetResourcesAsync();
        Task SaveResourceAsync(ClusterResource resource);
    }
}
