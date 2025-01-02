using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Ingress
{
    /// <summary>
    /// Configuration for reverse proxy
    /// </summary>
    public class IngressServiceDescription
    {
        public required string Id { get; set; }

        /// <summary>
        /// nginx server
        /// </summary>
        public required string IngressIp { get; set; }

        /// <summary>
        /// nginx port
        /// </summary>
        public required int IngressPort { get; set; }

        /// <summary>
        /// Domain
        /// </summary>
        public required string DomainName { get; set; }

        /// <summary>
        /// Settings for the reverse proxy
        /// </summary>
        public ReverseProxySettings? ReverseProxy { get; set; }

        /// <summary>
        /// Region specific ingress
        /// This allows multiple ingress nodes with the same domain name.
        /// Only DNS servers 
        /// </summary>
        public string? Region { get; set; }

        /// <summary>
        /// http or https
        /// </summary>
        public string Scheme { get; set; } = "https";

        /// <summary>
        /// Application server IP
        /// </summary>
        public required string DestinationIp { get; set; }

        /// <summary>
        /// Application server port
        /// </summary>
        public required int DestinationPort { get; set; }

        // Keys
        public byte[]? PemKey { get; set; }
        public byte[]? PemCert { get; set; }
    }
}
