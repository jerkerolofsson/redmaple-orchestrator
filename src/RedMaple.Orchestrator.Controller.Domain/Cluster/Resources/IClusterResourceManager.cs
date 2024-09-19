using RedMaple.Orchestrator.Contracts.Resources;

namespace RedMaple.Orchestrator.Controller.Domain.Cluster.Resources
{
    public interface IClusterResourceManager
    {
        Task SaveResourceAsync(ClusterResource resource);
        Task AddResourceAsync(ClusterResource resource);
        Task<ClusterResource?> GetClusterResourceAsync(string id);
        Task<List<ClusterResource>> GetClusterResourcesAsync();
        Task RemoveResourceAsync(ClusterResource resource);
        Task<List<ClusterResource>> GetClusterResourcesAsync(Dictionary<string, string> environmentVariables);
    }
}