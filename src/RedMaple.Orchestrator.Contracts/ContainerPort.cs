using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts
{
    public class ContainerPort
    {
        public ushort PrivatePort { get; set; }
        public string? Ip { get; set; }
        public string? Type { get; set; }
        public ushort PublicPort { get; set; }
    }
}
