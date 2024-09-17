using MediatR;
using RedMaple.Orchestrator.Contracts.Resources;
using RedMaple.Orchestrator.Controller.Domain.Deployments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Domain.Cluster.Resources.Integrations
{
    internal class AddApplicationResourceWhenReady : INotificationHandler<AppDeploymentReadyNotification>
    {
        private readonly IClusterResourceManager _resources;

        public AddApplicationResourceWhenReady(IClusterResourceManager resources)
        {
            _resources = resources;
        }
        public async Task Handle(AppDeploymentReadyNotification notification, CancellationToken cancellationToken)
        {
            var env = new Dictionary<string, string>();

            if (notification.Deployment.ApplicationProtocol is not null)
            {
                var name = notification.Deployment.Slug.ToLower();
                var port = notification.Deployment.ApplicationServerPort;
                var protocol = notification.Deployment.ApplicationProtocol.ToLower();
                var key = $"services__{name}__{protocol}__0";
                env[key] = $"{protocol}://{notification.Deployment.ApplicationServerIp}:{port}";
            }

            var resource = new ClusterResource
            {
                Id = "app__" + notification.Deployment.Id,
                Kind = ResourceKind.ApplicationService,
                Name = notification.Deployment.Slug,
                IsGlobal = false,
                Persist = false,
                EnvironmentVariables = env
            };

            await _resources.AddResourceAsync(resource);
        }
    }
}
