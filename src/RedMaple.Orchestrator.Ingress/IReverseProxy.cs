
using RedMaple.Orchestrator.Contracts;

namespace RedMaple.Orchestrator.Ingress
{
    public interface IReverseProxy
    {
        Task StartAsync();
        Task StopAsync();
        Task UpdateConfigurationAsync(List<IngressServiceDescription> services);
    }
}