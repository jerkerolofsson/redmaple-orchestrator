using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using MediatR;

using Microsoft.Extensions.Logging;

using RedMaple.Orchestrator.Contracts.Deployments;
using RedMaple.Orchestrator.Contracts.Node;
using RedMaple.Orchestrator.Controller.Domain.Node;

namespace RedMaple.Orchestrator.Controller.Domain.Deployments.Reliability
{
    internal class DeploymentReliabilityController : 
        IProgress<string>,
        INotificationHandler<AppDeploymentUnhealthyNotification>
    {
        private readonly ILogger<DeploymentReliabilityController> _logger;
        private readonly IDeploymentManager _deploymentManager;
        private readonly INodeManager _nodeManager;

        public DeploymentReliabilityController(
            ILogger<DeploymentReliabilityController> logger,
            IDeploymentManager deploymentManager,
            INodeManager nodeManager)
        {
            _logger = logger;
            _deploymentManager = deploymentManager;
            _nodeManager = nodeManager;
        }

        private readonly TimeSpan _timeDownBeforeFirstAttempt = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _timeDownBeforeRetry = TimeSpan.FromMinutes(5);

        public async Task Handle(AppDeploymentUnhealthyNotification notification, CancellationToken cancellationToken)
        {
            if (notification.Plan.HealthStatusChangedTimestamp is not null)
            {
                var elapsed = DateTime.UtcNow - notification.Plan.HealthStatusChangedTimestamp.Value;
                _logger.LogDebug("Deployment {DeploymentSlug} has been unhealthy for {elapsed} seconds", notification.Plan.Slug, elapsed.TotalSeconds);

                if (elapsed > _timeDownBeforeFirstAttempt)
                {
                    if (notification.Plan.ReliabiltityMitigationAttemptedTimestamp is null)
                    {
                        await TryFixDeploymentAsync(notification.Plan);
                    }
                    else
                    {
                        var timeSinceMitigation = DateTime.UtcNow - notification.Plan.ReliabiltityMitigationAttemptedTimestamp;
                        if (timeSinceMitigation > _timeDownBeforeRetry)
                        {
                            await TryFixDeploymentAsync(notification.Plan);
                        }
                    }
                }
            }
        }

        private async Task TryFixDeploymentAsync(DeploymentPlan plan)
        {
            _logger.LogInformation("Attempting to resolve unhealthy deployment {DeploymentSlug}", plan.Slug);
            plan.ReliabiltityMitigationAttemptedTimestamp = DateTime.UtcNow;
            await _deploymentManager.SaveAsync(plan);   

            var isIngressDown = await IsIngressDownAsync(plan.IngressServerIp);
            if (isIngressDown)
            {
                _logger.LogInformation("Ingress server {IngressServerIp} is down, trying to find alternative", plan.IngressServerIp);
                await FindAlternativeIngressNodeAsync(plan);
            }
            else if(plan.ApplicationServerIps.Count > 0)
            {
                await FindAlternativeApplicationNodeAsync(plan);
            }
        }


        private async Task<NodeInfo?> FindAlternativeApplicationNodeAsync(DeploymentPlan plan)
        {
            var applicationServerIp = plan.ApplicationServerIps.First();
            var appServers = plan.ApplicationServerIps.Where(x => x != applicationServerIp).ToArray();
            if (appServers.Length > 0)
            {
                System.Random.Shared.Shuffle(appServers);
                var newAppServer = appServers[0];

                _logger.LogInformation("Changing app server from {OldAppServerIp} to {NewAppServerIp}", applicationServerIp, newAppServer);
                plan.ApplicationServerIps = [newAppServer];
                await _deploymentManager.SaveAsync(plan);
                await _deploymentManager.RestartAsync(plan, this, CancellationToken.None);
            }
            else
            {
                _logger.LogWarning("No alternative app servers available for the deployment: {DeploymentSlug}, restarting..", plan.Slug);
                await _deploymentManager.RestartAsync(plan, this, CancellationToken.None);
            }
            return null;
        }

        private async Task<NodeInfo?> FindAlternativeIngressNodeAsync(DeploymentPlan plan)
        {
            var ingressNodes = (await _nodeManager.GetNodesAsync()).Where(x=>x.IsIngressEnabled).ToArray();
            if(ingressNodes.Length > 0)
            {
                System.Random.Shared.Shuffle(ingressNodes);
                var newIngressNode = ingressNodes[0];

                _logger.LogInformation("Changing ingress server from {OldIngressServerIp} to {NewIngressServerIp}", plan.IngressServerIp, newIngressNode.IpAddress);
                plan.IngressServerIp = newIngressNode.IpAddress;
                await _deploymentManager.SaveAsync(plan);
                await _deploymentManager.RestartAsync(plan, this, CancellationToken.None);
            }
            else
            {
                _logger.LogWarning("No alternative ingress servers available for the deployment: {DeploymentSlug}", plan.Slug);
            }
            return null;
        }

        private async Task<bool> IsIngressDownAsync(string? ingressServerIp)
        {
            if(!string.IsNullOrWhiteSpace(ingressServerIp))
            { 
                if(await _nodeManager.GetNodeByIpAddressAsync(ingressServerIp) == null)
                {
                    return true;
                }
            }
            return false;
        }

        public void Report(string value)
        {
            _logger.LogInformation(value);
        }
    }
}
