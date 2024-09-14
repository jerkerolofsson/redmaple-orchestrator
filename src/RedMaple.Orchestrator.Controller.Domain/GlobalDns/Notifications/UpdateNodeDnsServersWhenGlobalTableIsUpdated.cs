using MediatR;
using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.Contracts.Dns;
using RedMaple.Orchestrator.Controller.Domain.Node;
using RedMaple.Orchestrator.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Domain.GlobalDns.Notifications
{
    public record class GlobalDnsTableUpdatedNotification(List<DnsEntry> Entries) : INotification;

    internal class UpdateNodeDnsServersWhenGlobalTableIsUpdated : INotificationHandler<GlobalDnsTableUpdatedNotification>
    {
        private readonly ILogger _logger;
        private readonly INodeManager _nodeManager;

        public UpdateNodeDnsServersWhenGlobalTableIsUpdated(
            ILogger<UpdateNodeDnsServersWhenGlobalTableIsUpdated> logger,
            INodeManager nodeManager)
        {
            _logger = logger;
            _nodeManager = nodeManager;
        }

        public async Task Handle(GlobalDnsTableUpdatedNotification notification, CancellationToken cancellationToken)
        {
            foreach(var node in await _nodeManager.GetNodesAsync())
            {
                _logger.LogInformation("Updating global DNS table on {BaseUrl}..", node.BaseUrl);
                try
                {
                    using var dnsClient = new NodeDnsClient(node.BaseUrl);
                    var entries = await dnsClient.GetDnsEntriesAsync();
                    entries.RemoveAll(x => x.IsGlobal);

                    _logger.LogInformation("Adding {dnsEntryCount} entries to {BaseUrl}..", entries.Count, node.BaseUrl);
                    entries.AddRange(notification.Entries);
                    await dnsClient.SetDnsEntriesAsync(entries);
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "Failed to update DNS entries on {BaseUrl}", node.BaseUrl);
                }
            }
        }
    }
}
