using RedMaple.Orchestrator.Contracts.Deployments;
using RedMaple.Orchestrator.Contracts.Node;

namespace RedMaple.Orchestrator.Controller.Domain.Metrics.ContainerMetrics
{
    public interface IContainerStatsCollectorManager
    {
        Task UpdateCollectorsForDeploymentAsync(Deployment deployment);

        /// <summary>
        /// Updates the list of containers from the node
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        Task UpdateCollectorsForNodeAsync(NodeInfo node);
    }
}