using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Deployments
{
    public class DeploymentPlanTemplate
    {
        public required string IconUrl { get; set; }

        public string ApplicationProtocol { get; set; } = "https";

        /// <summary>
        /// Name of the deployment
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Kind of deployment
        /// </summary>
        public DeploymentKind Kind { get; set; } = DeploymentKind.DockerCompose;

        /// <summary>
        /// Create an ingress proxy
        /// </summary>
        public bool CreateIngress { get; set; } = true;

        /// <summary>
        /// Format depends on kind
        /// for docker compose this is the docker-compose.yaml
        /// </summary>
        public required string Plan { get; set; }
    }
}
