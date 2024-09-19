using MediatR;
using RedMaple.Orchestrator.Contracts.Resources;
using RedMaple.Orchestrator.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Domain.Cluster.Resources.Integrations
{
    internal class AddDnsResourceWhenNodeEnrolls : INotificationHandler<NodeEnrolledNotification>
    {
        private readonly IClusterResourceManager _resources;

        public AddDnsResourceWhenNodeEnrolls(IClusterResourceManager resources)
        {
            _resources = resources;
        }

        public async Task Handle(NodeEnrolledNotification notification, CancellationToken cancellationToken)
        {
            if (notification.Node.IsDnsEnabled && notification.Node.IpAddress is not null)
            {
                var dnsResource = new ClusterResource
                {
                    Id = "dns__" + notification.Node.IpAddress,
                    Slug = SlugGenerator.Generate("dns__" + notification.Node.IpAddress),
                    Kind = ResourceKind.Dns,
                    Name = notification.Node.IpAddress,
                    IsGlobal = true,
                    Persist = false,
                    EnvironmentVariables = new Dictionary<string, string>
                    {
                        ["REDMAPLE_DNS"] = notification.Node.IpAddress
                    }
                };

                await _resources.AddResourceAsync(dnsResource);
            }
        }
    }
}
