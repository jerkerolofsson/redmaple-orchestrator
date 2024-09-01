using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts
{
    public class IngressServiceDescription
    {
        public required string Id { get; set; }
        public required int IngressPort { get; set; }
        public required string DomainName { get; set; }

        /// <summary>
        /// http or https
        /// </summary>
        public string Schema { get; set; } = "https";
        public required string DestinationIp { get; set; }
        public required int DestinationPort { get; set; }
    }
}
