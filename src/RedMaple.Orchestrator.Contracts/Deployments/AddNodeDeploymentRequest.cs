using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Deployments
{
    public class AddNodeDeploymentRequest
    {
        /// <summary>
        /// Deployment slug
        /// </summary>
        public required string Slug { get; set; }

        /// <summary>
        /// HTTPS certificate (pfx)
        /// </summary>
        public required byte[] HttpsCertificatePfx { get; set; }

        /// <summary>
        /// Environment variables (set by the user when creating the deployment), and some automatic
        /// </summary>
        public required Dictionary<string, string> EnvironmentVariables { get; set; }

        public required DeploymentKind Kind { get; set; }

        /// <summary>
        /// YAML docker compose etc
        /// </summary>
        public required string Plan { get; set; }
    }
}
