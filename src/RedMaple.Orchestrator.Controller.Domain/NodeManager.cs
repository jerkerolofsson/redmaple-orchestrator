using RedMaple.Orchestrator.Contracts.Node;

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

        public async Task<NodeInfo?> GetNodeByIpAddressAsync(string ingressIp)
        {
            var nodes = await GetNodesAsync();
            foreach (var node in nodes)
            {
                if (node.IpAddress == ingressIp)
                {
                    return node;
                }
                if (node.HostAddresses is not null)
                {
                    foreach (var hostAddress in node.HostAddresses)
                    {
                        if (hostAddress == ingressIp)
                        {
                            return node;
                        }
                    }
                }
            }
            return null;
        }

        public Task<List<NodeInfo>> GetNodesAsync()
        {
            return Task.FromResult(_nodeRepository.Nodes.ToList());
        }
    }
}
