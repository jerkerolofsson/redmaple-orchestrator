using RedMaple.Orchestrator.Contracts;
using RedMaple.Orchestrator.Dns;
using RedMaple.Orchestrator.Ingress;

namespace RedMaple.Orchestrator.Controller.Domain
{
    public class IngressManager : IIngressManager
    {
        private readonly IIngressRepository _repository;
        private readonly IDns _dns;
        private readonly IReverseProxy _proxy;

        public IngressManager(IIngressRepository nodeRepository, IDns dns, IReverseProxy proxy)
        {
            _repository = nodeRepository;
            _dns = dns;
            _proxy = proxy;
        }

        public async Task AddIngressServiceAsync(IngressServiceDescription service)
        {
            // Validate

            // Save the entry in db
            await _repository.AddIngressServiceAsync(service);

            // Add DNS entry
            var ingressIpAddress = Environment.GetEnvironmentVariable("INGRESS_IP") ?? throw new InvalidDataException("INGRESS_IP environment variable is missing");

            await _dns.AddEntryAsync(service.DomainName, ingressIpAddress);
            await _dns.StopAsync();
            await _dns.StartAsync();

            // Add proxy entry forwarding from ingressIpAddress => service.DestinationIp
            await _proxy.StartAsync();
            await _proxy.UpdateConfigurationAsync(_repository.Services.ToList());
        }

        public Task<List<IngressServiceDescription>> GetServicesAsync()
        {
            return Task.FromResult(_repository.Services.ToList());
        }
    }
}