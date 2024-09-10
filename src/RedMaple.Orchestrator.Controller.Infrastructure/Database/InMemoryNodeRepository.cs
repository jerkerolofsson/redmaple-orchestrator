using RedMaple.Orchestrator.Contracts.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Infrastructure.Database
{
    internal class InMemoryNodeRepository : INodeRepository
    {
        private List<NodeInfo> _nodes = new();

        public IQueryable<NodeInfo> Nodes => _nodes.AsQueryable();

        public Task AddNodeAsync(NodeInfo nodeInfo)
        {
            _nodes.RemoveAll(x => x.Id == nodeInfo.Id);

            _nodes.Add(nodeInfo);
            return Task.CompletedTask;
        }
    }
}
