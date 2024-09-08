namespace RedMaple.Orchestrator.Contracts.Ingress
{
    public interface IIngressServiceController
    {
        Task<List<IngressServiceDescription>> GetServicesAsync();
        Task DeleteIngressServiceAsync(string id);
        Task AddIngressServiceAsync(IngressServiceDescription service);
    }
}