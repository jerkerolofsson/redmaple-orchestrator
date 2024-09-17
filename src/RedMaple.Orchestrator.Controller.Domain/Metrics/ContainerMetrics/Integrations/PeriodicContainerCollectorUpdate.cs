using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.Controller.Domain.Cluster;
using RedMaple.Orchestrator.Controller.Domain.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Domain.Metrics.ContainerMetrics.Integrations
{
    /// <summary>
    /// Periodically checks running containers
    /// </summary>
    internal class PeriodicContainerCollectorUpdate : BackgroundService
    {
        private readonly ILogger<PeriodicContainerCollectorUpdate> _logger;
        private readonly INodeManager _nodeManager;
        private readonly IContainerStatsCollectorManager _manager;

        public PeriodicContainerCollectorUpdate(
            ILogger<PeriodicContainerCollectorUpdate> logger,
            INodeManager nodeManager,
            IContainerStatsCollectorManager anager)
        {
            _logger = logger;
            _nodeManager = nodeManager;
            _manager = anager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                try
                {
                    var nodes = await _nodeManager.GetNodesAsync();
                    foreach (var node in nodes)
                    {
                        await _manager.UpdateCollectorsForNodeAsync(node);
                    }
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "Failed to update");
                }
            }
        }
    }
}
