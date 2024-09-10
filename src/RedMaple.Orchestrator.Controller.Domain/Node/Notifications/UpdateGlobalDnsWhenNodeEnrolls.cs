using MediatR;
using RedMaple.Orchestrator.Contracts.Dns;
using RedMaple.Orchestrator.Contracts.Node;
using RedMaple.Orchestrator.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Domain.Node.Notifications
{
    public record class NodeEnrolledNotification(NodeInfo Node) : INotification;

    /// <summary>
    /// Updates the node's global DNS entries when it enrolls (to handle DNS changes if the node was offline)
    /// </summary>
    internal class UpdateGlobalDnsWhenNodeEnrolls : INotificationHandler<NodeEnrolledNotification>
    {
        private readonly IGlobalDnsRepository _globalDns;

        public UpdateGlobalDnsWhenNodeEnrolls(IGlobalDnsRepository globalDns)
        {
            _globalDns = globalDns;
        }

        public async Task Handle(NodeEnrolledNotification notification, CancellationToken cancellationToken)
        {
            var globalEntries = await _globalDns.GetDnsEntriesAsync();

            using var dnsClient = new NodeDnsClient(notification.Node.BaseUrl);
            var entries = await dnsClient.GetDnsEntriesAsync();
            entries.RemoveAll(x => x.IsGlobal);
            entries.AddRange(globalEntries);
            await dnsClient.SetDnsEntriesAsync(entries);
        }
    }
}
