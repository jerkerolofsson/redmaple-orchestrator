using MediatR;
using RedMaple.Orchestrator.Contracts.Dns;
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
        private readonly INodeManager _nodeManager;

        public UpdateNodeDnsServersWhenGlobalTableIsUpdated(INodeManager nodeManager)
        {
            _nodeManager = nodeManager;
        }

        public async Task Handle(GlobalDnsTableUpdatedNotification notification, CancellationToken cancellationToken)
        {
            foreach(var node in await _nodeManager.GetNodesAsync())
            {
                using var dnsClient = new NodeDnsClient(node.BaseUrl);
                var entries = await dnsClient.GetDnsEntriesAsync();
                entries.RemoveAll(x => x.IsGlobal);
                entries.AddRange(notification.Entries);
                await dnsClient.SetDnsEntriesAsync(entries);
            }
        }
    }
}
