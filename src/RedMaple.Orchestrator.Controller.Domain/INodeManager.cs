using RedMaple.Orchestrator.Contracts;

namespace RedMaple.Orchestrator.Controller.Domain
{
    public interface INodeManager
    {
        Task<List<NodeInfo>> GetNodesAsync();
        Task EnrollNodeAsync(NodeInfo nodeInfo);
    }
}