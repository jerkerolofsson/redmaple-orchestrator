using RedMaple.Orchestrator.Contracts.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Containers
{
    public record class DeployedContainer(NodeInfo Node, string ApplicationServerIp, int? ApplicationServerPort, Container Container);
}
