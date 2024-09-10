using RedMaple.Orchestrator.Contracts.Node;

namespace RedMaple.Orchestrator.Controller.Domain.Node
{
    public interface INodeManager
    {
        /// <summary>
        /// Returns all nodes
        /// </summary>
        /// <returns></returns>
        Task<List<NodeInfo>> GetNodesAsync();

        /// <summary>
        /// Called from the API when a node is started
        /// </summary>
        /// <param name="nodeInfo"></param>
        /// <returns></returns>
        Task EnrollNodeAsync(NodeInfo nodeInfo);

        /// <summary>
        /// Returns a node from an IP address
        /// </summary>
        /// <param name="ingressIp"></param>
        /// <returns></returns>
        Task<NodeInfo?> GetNodeByIpAddressAsync(string ingressIp);
    }
}