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
    internal class UpdateCollectorsWhenAppIsStopping : INotificationHandler<AppDeploymentStoppingNotification>
    {
        private readonly IContainerStatsCollectorManager _manager;

        public UpdateCollectorsWhenAppIsStopping(IContainerStatsCollectorManager anager)
        {
            _manager = anager;
        }

        public async Task Handle(AppDeploymentStoppingNotification notification, CancellationToken cancellationToken)
        {
            await _manager.UpdateCollectorsForDeploymentAsync(notification.Deployment);
        }
    }
}
