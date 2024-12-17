using RedMaple.Orchestrator.Contracts.Infra;

namespace RedMaple.Orchestrator.Controller.Domain.Cluster.Infra
{
    public interface IInfraResourceManager
    {
        Task SaveResourceAsync(InfraResource resource);
        Task AddResourceAsync(InfraResource resource);
        Task<InfraResource?> GetInfraResourceAsync(string id);
        Task<List<InfraResource>> GetInfraResourcesAsync();
        Task RemoveResourceAsync(InfraResource resource);
    }
}