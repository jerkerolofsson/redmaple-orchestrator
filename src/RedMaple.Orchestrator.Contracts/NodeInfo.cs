using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts
{
    public class NodeInfo
    {
        public required string Id { get; set; }
        public string? IpAddress { get; set; }
    }
}
