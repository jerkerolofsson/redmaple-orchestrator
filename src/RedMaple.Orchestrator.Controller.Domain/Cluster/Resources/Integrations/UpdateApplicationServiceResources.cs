using Docker.DotNet.Models;
using MediatR;
using RedMaple.Orchestrator.Contracts.Resources;
using RedMaple.Orchestrator.Controller.Domain.Deployments;
using RedMaple.Orchestrator.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Domain.Cluster.Resources.Integrations
{
    internal class RemoveApplicationResourceWhenStopping : INotificationHandler<AppDeploymentStoppingNotification>
    {
        private readonly IClusterResourceManager _resources;

        public RemoveApplicationResourceWhenStopping(IClusterResourceManager resources)
        {
            _resources = resources;
        }
        public async Task Handle(AppDeploymentStoppingNotification notification, CancellationToken cancellationToken)
        {
            var resourceId = "app__" + notification.Deployment.Id;
            var resource = await _resources.GetClusterResourceAsync(resourceId);
            if (resource is not null)
            {
                await _resources.RemoveResourceAsync(resource);
            }
        }
    }

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
                var name = notification.Deployment.Slug;
                var port = notification.Deployment.ApplicationServerPort;
                var protocol = notification.Deployment.ApplicationProtocol.ToLower();
                var key = $"services__{name}__{protocol}__0";
                env[key] = $"{protocol}://{notification.Deployment.ApplicationServerIp}:{port}";
            }

            var resourceId = "app__" + notification.Deployment.Id;
            var slug = notification.Deployment.Slug;
            var resource = new ClusterResource
            {
                Id = resourceId,
                Slug = slug,
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
