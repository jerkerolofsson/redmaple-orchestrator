using RedMaple.Orchestrator.Contracts;

namespace RedMaple.Orchestrator.Controller.Domain
{
    public interface IIngressManager
    {
        Task<List<IngressServiceDescription>> GetServicesAsync();
        Task AddIngressServiceAsync(IngressServiceDescription service);
    }
}