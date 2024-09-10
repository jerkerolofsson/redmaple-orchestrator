using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Dns
{
    public class DnsEntry
    {
        public required string Hostname { get; set; }
        public required string IpAddress { get; set; }

        /// <summary>
        /// Global DNS entries are managed in a separate UI
        /// </summary>
        public bool IsGlobal { get; set; }
    }
}
