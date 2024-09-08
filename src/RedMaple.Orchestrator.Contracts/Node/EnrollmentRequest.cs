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
    }
}
