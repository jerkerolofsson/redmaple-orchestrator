﻿using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.Contracts.Dns;
using RedMaple.Orchestrator.Contracts.Ingress;
using RedMaple.Orchestrator.Controller.Domain.GlobalDns;
using RedMaple.Orchestrator.Controller.Domain.Node;
using RedMaple.Orchestrator.Sdk;
using RedMaple.Orchestrator.Security.Services;
using System;
using System.Net;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace RedMaple.Orchestrator.Controller.Domain.Ingress
{
    public class IngressManager : IIngressManager
    {
        private readonly ILogger<IngressManager> _logger;
        private readonly ICertificateAuthority _certificateAuthority;
        private readonly IGlobalDns _globalDns;
        private readonly IIngressRepository _repository;
        private readonly INodeManager _nodeManager;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);

        public IngressManager(
            ILogger<IngressManager> logger,
            ICertificateAuthority certificateAuthority,
            IGlobalDns globalDns,
            IIngressRepository nodeRepository,
            INodeManager nodeManager)
        {
            _logger = logger;
            _certificateAuthority = certificateAuthority;
            _globalDns = globalDns;
            _repository = nodeRepository;
            _nodeManager = nodeManager;
        }

        public async Task AddIngressServiceAsync(IngressServiceDescription service, IProgress<string> progress)
        {
            progress.Report($"Configuring ingress-server {service.DomainName} for {service.Id}..");

            await _lock.WaitAsync();
            try 
            { 

                // Validate
                var node = await _nodeManager.GetNodeByIpAddressAsync(service.IngressIp);
                if (node is null)
                {
                    var error = $"Ingress node not found at the specified IP";
                    _logger.LogError(error);
                    progress.Report(error);
                    throw new Exception(error);
                }

                await DeleteExistingIngressServicesWithSameDomainName(service);

                _logger.LogInformation("Adding ingress-service {ServiceId}", service.Id);

                _logger.LogDebug("Generating certificate for ingress-service {ServiceId}", service.Id);
                var certificate = await _certificateAuthority.GetOrCreateCertificateAsync(service.DomainName);
                if (certificate?.PemCertPath is null || certificate?.PemKeyPath is null)
                {
                    throw new Exception($"CA error");
                }
                service.PemCert = await File.ReadAllBytesAsync(certificate.PemCertPath);
                service.PemKey = await File.ReadAllBytesAsync(certificate.PemKeyPath);

                // Save the entry in db
                await _repository.AddIngressServiceAsync(service);

                // Add DNS entry
                await UpdateDnsServersAsync(service, progress);

                // Create reverse proxy
                _logger.LogInformation("Provisioning {BaseUrl} with ingress service {ServiceId}", node.BaseUrl, service.Id);
                progress.Report($"Provisioning ingress service {node.IpAddress}..");
                using var ingressClient = new NodeIngressClient(node.BaseUrl);
                await ingressClient.AddIngressServiceAsync(service, progress);
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task DeleteExistingIngressServicesWithSameDomainName(IngressServiceDescription service)
        {
            var services = await _repository.GetServicesAsync();
            foreach (var ingressService in services.Where(x => x.DomainName == service.DomainName || x.Id == service.Id))
            {
                await DeleteIngressServiceNoLockAsync(ingressService.Id);
            }
        }

        private async Task UpdateDnsServersAsync(IngressServiceDescription service, IProgress<string> progress)
        {
            var dnsEntry = new DnsEntry { Hostname = service.DomainName, IpAddress = service.IngressIp, IsGlobal = true };

            var dnsEntries = await _globalDns.GetDnsEntriesAsync();

            var existingEntry = dnsEntries.FirstOrDefault(x => x.Hostname == service.DomainName);
            if (existingEntry is null)
            {
                progress.Report($"Creating DNS entry {service.DomainName} to {service.IngressIp}..");
                _logger.LogWarning("Deployment plan DNS entry creeated for {DomainName}={IpAddress}", service.DomainName, service.IngressIp);
                dnsEntries.Add(new Contracts.Dns.DnsEntry { IsGlobal = true, Hostname = service.DomainName, IpAddress = service.IngressIp });
                await _globalDns.SetDnsEntriesAsync(dnsEntries);
            }
            else if (existingEntry.IpAddress != service.IngressIp)
            {
                progress.Report($"Updating DNS for {service.DomainName} from {existingEntry.IpAddress} to {service.IngressIp}..");

                _logger.LogWarning("Deployment plan DNS entry changed from {old} to {IpAddress}", existingEntry.IpAddress, service.IngressIp);
                existingEntry.IpAddress = service.IngressIp;
                await _globalDns.SetDnsEntriesAsync(dnsEntries);
            }
            else
            {
                _logger.LogDebug("DNS entry already exists,{DomainName}={IpAddress}", service.DomainName, service.IngressIp);
                await _globalDns.SetDnsEntriesAsync(dnsEntries);
            }

            /*
            dnsEntries.RemoveAll(x => x.Hostname == dnsEntry.Hostname);
            dnsEntries.Add(dnsEntry);
            await _globalDns.SetDnsEntriesAsync(dnsEntries);
            */
        }

        public async Task DeleteIngressServiceAsync(string id)
        {
            await _lock.WaitAsync();
            try
            {
                await DeleteIngressServiceNoLockAsync(id);
            }
            finally
            {
                _lock.Release();
            }

        }

        private async Task DeleteIngressServiceNoLockAsync(string id)
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
            if(service is null)
            {
                return;
            }
            var node = await _nodeManager.GetNodeByIpAddressAsync(service.IngressIp);
            if (node is not null)
            {
                using var ingressClient = new NodeIngressClient(node.BaseUrl);
                await ingressClient.DeleteIngressServiceAsync(service.Id);
            }
        }

        private async Task RemoveDnsEntryAsync(IngressServiceDescription service)
        {
            var entries = await _globalDns.GetDnsEntriesAsync();
            entries.RemoveAll(x => x.Hostname == service.DomainName);
            await _globalDns.SetDnsEntriesAsync(entries);
            /*
            foreach (var dnsNode in await _nodeManager.GetNodesAsync())
            {
                using var dnsClient = new NodeDnsClient(dnsNode.BaseUrl);
                var entries = await dnsClient.GetDnsEntriesAsync();
                entries.RemoveAll(x => x.Hostname == service.DomainName);
                await dnsClient.SetDnsEntriesAsync(entries);
            }*/
        }

        public async Task<List<IngressServiceDescription>> GetServicesAsync()
        {
            return await _repository.GetServicesAsync();
        }

        public async Task<bool> TryDeleteIngressServiceByDomainNameAsync(string? domainName)
        {
            if(domainName is null)
            {
                return false;
            }

            var services = await GetServicesAsync();
            var result = false;
            foreach (var service in await GetServicesAsync())
            {
                if (service.DomainName == domainName)
                {
                    await DeleteIngressServiceAsync(service.Id);
                    result = true;
                }
            }
            return result;
        }
    }
}