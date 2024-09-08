using RedMaple.Orchestrator.Contracts.Node;

namespace RedMaple.Orchestrator.Controller.Domain
{
    public interface INodeManager
    {
        Task<List<NodeInfo>> GetNodesAsync();
        Task EnrollNodeAsync(NodeInfo nodeInfo);
        Task<NodeInfo?> GetNodeByIpAddressAsync(string ingressIp);
    }
}