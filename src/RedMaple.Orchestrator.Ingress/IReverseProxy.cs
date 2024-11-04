using RedMaple.Orchestrator.Contracts.Ingress;

namespace RedMaple.Orchestrator.Ingress
{
    public interface IReverseProxy
    {
        Task StartAsync();
        Task StopAsync();
        Task UpdateConfigurationAsync(List<IngressServiceDescription> services, CancellationToken cancellationToken);
    }
}