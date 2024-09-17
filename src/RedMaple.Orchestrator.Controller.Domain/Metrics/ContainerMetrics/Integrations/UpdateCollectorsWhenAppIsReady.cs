using MediatR;
using RedMaple.Orchestrator.Controller.Domain.Cluster;
using RedMaple.Orchestrator.Controller.Domain.Deployments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Domain.Metrics.ContainerMetrics.Integrations
{
    internal class UpdateCollectorsWhenAppIsReady : INotificationHandler<AppDeploymentReadyNotification>
    {
        private readonly IContainerStatsCollectorManager _manager;

        public UpdateCollectorsWhenAppIsReady(IContainerStatsCollectorManager anager)
        {
            _manager = anager;
        }

        public async Task Handle(AppDeploymentReadyNotification notification, CancellationToken cancellationToken)
        {
            await _manager.UpdateCollectorsForDeploymentAsync(notification.Deployment);
        }
    }
}
