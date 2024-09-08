using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Node
{
    /// <summary>
    /// Contains information about a node communicated from the node to the controller
    /// </summary>
    public class NodeInfo
    {
        public required string Id { get; set; }

        public string Schema { get; set; } = "http";

        public string BaseUrl => $"{Schema}://{IpAddress}:{Port}";

        public int Port { get; set; } = 1889;

        /// <summary>
        /// List of host addresses, provided by the remote host
        /// These will not be accurate when running in a container so normally
        /// we will use the IpAddress, however, when the IpAddress is ::1 / 127.0.0.1
        /// we may use these
        /// </summary>
        public required List<string> HostAddresses { get; set; }

        /// <summary>
        /// This IP address is set from the remote IP when the node enrolls
        /// </summary>
        public string? IpAddress { get; set; }
    }
}
