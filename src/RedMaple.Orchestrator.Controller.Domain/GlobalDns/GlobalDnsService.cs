using MediatR;
using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.Contracts.Dns;
using RedMaple.Orchestrator.Controller.Domain.GlobalDns.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Domain.GlobalDns
{
    public class GlobalDnsService : IGlobalDns
    {
        private readonly IGlobalDnsRepository _repository;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);

        public GlobalDnsService(
            ILogger<GlobalDnsService> logger,
            IGlobalDnsRepository repository,
            IMediator ediator)
        {
            _logger = logger;
            _repository = repository;
            _mediator = ediator;
        }

        public async Task<List<DnsEntry>> GetDnsEntriesAsync()
        {
            return await _repository.GetDnsEntriesAsync();
        }

        public async Task<bool> TryDeleteDnsEntryByDomainNameAsync(string? domainName)
        {
            if(string.IsNullOrEmpty(domainName))
            {
                return false;
            }

            var entries = (await GetDnsEntriesAsync()).Where(x => x.Hostname == domainName).ToList();
            if(entries.Count > 0)
            {
                await _lock.WaitAsync();
                try
                {
                    entries.RemoveAll(x=>x.Hostname == domainName);
                    await _repository.SetDnsEntriesAsync(entries);
                }
                finally
                {
                    _lock.Release();
                }
                await SendNotificationAsync(entries);
            }
            return entries.Count > 0;
        }
        public async Task SetDnsEntriesAsync(List<DnsEntry> entries)
        {
            await _lock.WaitAsync();
            try
            {
                _logger.LogInformation("Updating global DNS table with {dnsEntryCount}..", entries.Count);
                EnsureAllEntriesAreGlobal(entries);

                await _repository.SetDnsEntriesAsync(entries);
            }
            finally
            {
                _lock.Release();
            }
            await SendNotificationAsync(entries);

        }

        private static void EnsureAllEntriesAreGlobal(List<DnsEntry> entries)
        {
            foreach (var entry in entries)
            {
                entry.IsGlobal = true;
            }
        }

        private async Task SendNotificationAsync(List<DnsEntry> entries)
        {
            var notification = new GlobalDnsTableUpdatedNotification(entries);
            await _mediator.Publish(notification);
        }

    }
}
