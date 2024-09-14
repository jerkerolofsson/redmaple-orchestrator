using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Deployments
{
    public class DeploymentStatus
    {
        /// <summary>
        /// Slug matching deployment
        /// </summary>
        public required string Slug { get; set; }
        public string? Status { get; set; } = "Unknown";
    }
}
