using RedMaple.Orchestrator.Contracts;

namespace RedMaple.Orchestrator.Controller.Domain
{
    public class NodeManager : INodeManager
    {
        private readonly INodeRepository _nodeRepository;

        public NodeManager(INodeRepository nodeRepository)
        {
            _nodeRepository = nodeRepository;
        }

        public async Task EnrollNodeAsync(NodeInfo nodeInfo)
        {
            await _nodeRepository.AddNodeAsync(nodeInfo);
        }

        public Task<List<NodeInfo>> GetNodesAsync()
        {
            return Task.FromResult(_nodeRepository.Nodes.ToList());
        }
    }
}
