using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Infra
{
    /// <summary>
    /// Server or similar
    /// </summary>
    public class InfraResource
    {
        /// <summary>
        /// Flag to indicate the the resource is healthy
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// ID
        /// </summary>
        public required string Id { get; set; }

        /// <summary>
        /// Name of the resource
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Category/Grouping
        /// </summary>
        public required string Category { get; set; }

        /// <summary>
        /// Location
        /// </summary>
        public string? Region { get; set; }

        /// <summary>
        /// Public IP of server
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// Login username
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// Login password
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// OS
        /// </summary>
        public string? OperatingSystem { get; set; }

        /// <summary>
        /// Type
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Name of responsible user
        /// </summary>
        public string? ResponsibleUser { get; set; }
    }
}
