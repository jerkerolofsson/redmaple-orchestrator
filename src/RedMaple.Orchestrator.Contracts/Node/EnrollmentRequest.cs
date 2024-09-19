using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Node
{
    /// <summary>
    /// Information about a running node
    /// </summary>
    public class EnrollmentRequest
    {
        public required string Id { get; set; }
        public int Port { get; set; } = 1889;
        public required List<string> HostAddresses { get; set; }
        public string Schema { get; set; } = "http";
        public int IngressHttpsPort { get; set; }
        public bool IsDnsEnabled { get; set; }
        public bool IsIngressEnabled { get; set; }
        public bool IsLoadBalancerEnabled { get; set; }
        public bool IsApplicationHostEnabled { get; set; }
        public required string Tags { get; set; }
        public required string Name { get; set; }
        public required string Region { get; set; }
    }
}
