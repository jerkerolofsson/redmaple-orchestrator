using Docker.DotNet.Models;
using MediatR;

using RedMaple.Orchestrator.Contracts.Deployments;
using RedMaple.Orchestrator.Contracts.Resources;
using RedMaple.Orchestrator.Controller.Domain.Deployments;
using RedMaple.Orchestrator.DockerCompose;
using RedMaple.Orchestrator.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
            await DeleteResourceAsync(notification.Deployment, notification.Plan);
        }

        private async Task DeleteResourceAsync(Deployment deployment, DeploymentPlan plan)
        {
            var resourceId = "app__" + deployment.Id;
            var resource = await _resources.GetClusterResourceAsync(resourceId);
            if (resource is not null)
            {
                await _resources.RemoveResourceAsync(resource);
            }

            foreach(var planResource in await _resources.GetClusterResourcesByPlanAsync(plan.Id))
            {
                await _resources.RemoveResourceAsync(planResource);
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

        private static string? ReadVersionFromImage(DeploymentPlan plan)
        {
            try
            {
                var composePlan = DockerComposeParser.ParseYaml(plan.Plan);
                if (composePlan?.services is not null)
                {
                    foreach (var service in composePlan.services.Values)
                    {
                        if (!string.IsNullOrEmpty(service.image))
                        {
                            var imageParts = service.image.Split(':');
                            if (imageParts.Length > 0)
                            {
                                return imageParts[^1];
                            }
                        }
                    }
                    return "latest";
                }
            }
            catch { }
            return null;
        }

        public async Task Handle(AppDeploymentReadyNotification notification, CancellationToken cancellationToken)
        {
            var env = new Dictionary<string, string>();



            var name = notification.Deployment.Slug;
            if (!string.IsNullOrEmpty(notification.Deployment.Resource?.ServiceName))
            {
                name = notification.Deployment.Resource.ServiceName;
            }

            if (notification.Deployment.ApplicationProtocol is not null)
            {
                var port = notification.Deployment.ApplicationServerPort;
                var protocol = "tcp";
                if (notification.Deployment.ApplicationProtocol is not null)
                {
                    protocol = notification.Deployment.ApplicationProtocol.ToLower();
                }
                var key = $"services__{name}__{protocol}__0";
                if (string.IsNullOrEmpty(notification.Plan.DomainName) || !notification.Plan.CreateIngress)
                {
                    env[key] = $"{protocol}://{notification.Deployment.ApplicationServerIp}:{port}";
                }
                else
                {
                    env[key] = $"https://{notification.Plan.DomainName}";
                }
            }

            var version = ReadVersionFromImage(notification.Plan);
            if(version is not null)
            {
                env[$"version__{name}"] = version;
            }

            // Add resource variables
            var resourceOptions = notification.Plan.Resource;
            if (resourceOptions?.Create == true && resourceOptions?.Exported is not null)
            {
                foreach (var exportEnv in resourceOptions.Exported)
                {
                    if (notification.Deployment.EnvironmentVariables.TryGetValue(exportEnv, out var value))
                    {
                        env[exportEnv] = value;
                    }
                }

                if(!string.IsNullOrEmpty(resourceOptions.ConnectionStringFormat) && notification.Deployment.EnvironmentVariables is not null)
                {
                    var envVariables = new Dictionary<string, string?>(notification.Deployment.EnvironmentVariables.Select(x=> new KeyValuePair<string, string?>(x.Key, x.Value)));

                    var connectionStringName = $"ConnectionStrings__{notification.Deployment.Slug}";
                    if(resourceOptions.ConnectionStringVariableNameFormat is not null)
                    {
                        connectionStringName = DockerComposeParser.ReplaceEnvironmentVariables(resourceOptions.ConnectionStringVariableNameFormat, envVariables);
                    }
                    var connectionString = DockerComposeParser.ReplaceEnvironmentVariables(resourceOptions.ConnectionStringFormat, envVariables);
                    env[connectionStringName] = connectionString;
                }

                var resourceId = "app__" + notification.Plan.Id;
                var slug = notification.Deployment.Slug;
                var resource = new ClusterResource
                {
                    Id = resourceId,
                    PlanId = notification.Plan.Id,
                    IconUrl = notification.Plan?.IconUrl,
                    Slug = slug,
                    Kind = resourceOptions.Kind ?? ResourceKind.ApplicationService,
                    Name = notification.Deployment.Slug,
                    IsGlobal = false,
                    Persist = false,
                    EnvironmentVariables = env,
                    ResourceRegion = notification.Plan?.Region
                };

                await _resources.AddResourceAsync(resource);
            }
        }
    }
}
