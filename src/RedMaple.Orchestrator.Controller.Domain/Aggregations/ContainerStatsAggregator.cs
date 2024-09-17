using RedMaple.Orchestrator.Contracts.Containers;
using RedMaple.Orchestrator.Contracts.Node;
using RedMaple.Orchestrator.Controller.Domain.Aggregations;
using RedMaple.Orchestrator.Controller.Domain.Metrics.ContainerMetrics;
using RedMaple.Orchestrator.Controller.Domain.Metrics.Models;
using RedMaple.Orchestrator.Controller.Domain.Node;
using RedMaple.Orchestrator.Sdk;
using System.Collections.Concurrent;

namespace RedMaple.Orchestrator.Controller.ViewModels
{
    public class ContainerStatsAggregator
    {
        private readonly IContainerStatsManager _statsManager;
        private readonly INodeManager _nodeManager;

        public ContainerStatsAggregator(IContainerStatsManager statsManager, INodeManager nodeManager)
        {
            _statsManager = statsManager;
            _nodeManager = nodeManager;
        }

        public async Task<IReadOnlyList<ContainerStatsAggregation>> GetNodeContainersAsync(string nodeId)
        {
            var nodes = await _nodeManager.GetNodesAsync();
            var nodeInfo = nodes.FirstOrDefault(x => x.Id == nodeId);
            if(nodeInfo is null)
            {
                throw new Exception("Node not found");
            }

            using var client = new NodeContainersClient(nodeInfo.BaseUrl);
            var containers = await client.GetContainersAsync();
            var result = new List<ContainerStatsAggregation>();
            foreach (var container in containers)
            {
                var aggregation = new ContainerStatsAggregation { Container = container };
                aggregation.Stats = _statsManager.GetContainerStats(container.Id);
                result.Add(aggregation);
            }
            return result;
        }
    }
}
