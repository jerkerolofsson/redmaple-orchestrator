using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts
{
    public interface INodeRepository
    {
        IQueryable<NodeInfo> Nodes { get; }

        Task AddNodeAsync(NodeInfo nodeInfo);
    }
}
