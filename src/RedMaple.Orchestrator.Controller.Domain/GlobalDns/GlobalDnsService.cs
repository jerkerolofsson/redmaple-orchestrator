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

        public async Task SetDnsEntriesAsync(List<DnsEntry> entries)
        {
            EnsureAllEntriesAreGlobal(entries);

            _logger.LogInformation("Updating global DNS table with {dnsEntryCount}..", entries.Count);
            await _repository.SetDnsEntriesAsync(entries);

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
