using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.Contracts.Dns;
using RedMaple.Orchestrator.Contracts.Ingress;
using RedMaple.Orchestrator.Controller.Domain.Node;
using RedMaple.Orchestrator.Sdk;
using RedMaple.Orchestrator.Security.Services;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace RedMaple.Orchestrator.Controller.Domain.Ingress
{
    public class IngressManager : IIngressManager
    {
        private readonly ILogger<IngressManager> _logger;
        private readonly ICertificateAuthority _certificateAuthority;
        private readonly IGlobalDnsRepository _globalDnsRepository;
        private readonly IIngressRepository _repository;
        private readonly INodeManager _nodeManager;

        public IngressManager(
            ILogger<IngressManager> logger,
            ICertificateAuthority certificateAuthority,
            IGlobalDnsRepository globalDnsRepository,
            IIngressRepository nodeRepository,
            INodeManager nodeManager)
        {
            _logger = logger;
            _certificateAuthority = certificateAuthority;
            _globalDnsRepository = globalDnsRepository;
            _repository = nodeRepository;
            _nodeManager = nodeManager;
        }

        public async Task AddIngressServiceAsync(IngressServiceDescription service)
        {
            // Validate
            var node = await _nodeManager.GetNodeByIpAddressAsync(service.IngressIp);
            if (node is null)
            {
                throw new Exception($"Ingress node not found at the specified IP");
            }

            var certificate = await _certificateAuthority.GetOrCreateCertificateAsync(service.DomainName);
            if (certificate?.PemCertPath is null || certificate?.PemKeyPath is null)
            {
                throw new Exception($"CA error");
            }

            service.PemCert = await File.ReadAllBytesAsync(certificate.PemCertPath);
            service.PemKey = await File.ReadAllBytesAsync(certificate.PemKeyPath);

            // Save the entry in db
            await _repository.AddIngressServiceAsync(service);
            _logger.LogInformation("Adding ingress-service {ServiceId}", service.Id);

            // Add DNS entry
            await UpdateDnsServersAsync(service);

            // Create reverse proxy
            using var ingressClient = new NodeIngressClient(node.BaseUrl);
            await ingressClient.AddIngressServiceAsync(service);
        }

        private async Task UpdateDnsServersAsync(IngressServiceDescription service)
        {
            var dnsEntry = new DnsEntry { Hostname = service.DomainName, IpAddress = service.IngressIp, IsGlobal = true };

            _logger.LogInformation("Adding DNS entry {Name}-{IpAddress}", service.DomainName, service.IngressIp);

            var dnsEntries = await _globalDnsRepository.GetDnsEntriesAsync();
            dnsEntries.RemoveAll(x => x.Hostname == dnsEntry.Hostname);
            dnsEntries.Add(dnsEntry);
            await _globalDnsRepository.SetDnsEntriesAsync(dnsEntries);

            /*
            foreach (var dnsNode in await _nodeManager.GetNodesAsync())
            {
                using var dnsClient = new NodeDnsClient(dnsNode.BaseUrl);
                var entries = await dnsClient.GetDnsEntriesAsync();
                entries.RemoveAll(x => x.Hostname == dnsEntry.Hostname);
                entries.Add(dnsEntry);

                await dnsClient.SetDnsEntriesAsync(entries);
            }*/
        }

        public async Task DeleteIngressServiceAsync(string id)
        {
            var services = await _repository.GetServicesAsync();
            var service = services.FirstOrDefault(x => x.Id == id);

            await _repository.DeleteIngressServiceAsync(id);

            // Remove DNS entry
            if (service is not null)
            {
                await RemoveDnsEntryAsync(service);
                await RemoveReverseProxyAsync(service);
            }
        }

        private async Task RemoveReverseProxyAsync(IngressServiceDescription? service)
        {
            var node = await _nodeManager.GetNodeByIpAddressAsync(service.IngressIp);
            if (node is not null)
            {
                using var ingressClient = new NodeIngressClient(node.BaseUrl);
                await ingressClient.DeleteIngressServiceAsync(service.Id);
            }
        }

        private async Task RemoveDnsEntryAsync(IngressServiceDescription service)
        {
            foreach (var dnsNode in await _nodeManager.GetNodesAsync())
            {
                using var dnsClient = new NodeDnsClient(dnsNode.BaseUrl);
                var entries = await dnsClient.GetDnsEntriesAsync();
                entries.RemoveAll(x => x.Hostname == service.DomainName);
                await dnsClient.SetDnsEntriesAsync(entries);
            }
        }

        public async Task<List<IngressServiceDescription>> GetServicesAsync()
        {
            return await _repository.GetServicesAsync();
        }
    }
}