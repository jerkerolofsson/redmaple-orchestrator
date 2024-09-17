using MediatR;
using RedMaple.Orchestrator.Controller.Domain.Cluster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Domain.Metrics.ContainerMetrics.Integrations
{
    internal class UpdateCollectorsWhenNodeEnrolls : INotificationHandler<NodeEnrolledNotification>
    {
        private readonly IContainerStatsCollectorManager _manager;

        public UpdateCollectorsWhenNodeEnrolls(IContainerStatsCollectorManager anager)
        {
            _manager = anager;
        }

        public async Task Handle(NodeEnrolledNotification notification, CancellationToken cancellationToken)
        {
            await _manager.UpdateCollectorsForNodeAsync(notification.Node);
        }
    }
}
