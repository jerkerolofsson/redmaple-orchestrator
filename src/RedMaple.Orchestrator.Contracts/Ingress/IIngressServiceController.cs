namespace RedMaple.Orchestrator.Contracts.Ingress
{
    public interface IIngressServiceController
    {
        Task<List<IngressServiceDescription>> GetServicesAsync();
        Task DeleteIngressServiceAsync(string id, CancellationToken cancellationToken);
        Task AddIngressServiceAsync(IngressServiceDescription service, IProgress<string> progress, CancellationToken cancellationToken);
    }
}