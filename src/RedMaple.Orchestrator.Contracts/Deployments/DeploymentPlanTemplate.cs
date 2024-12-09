using RedMaple.Orchestrator.Contracts.Healthz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Deployments
{
    public class DeploymentPlanTemplate
    {
        /// <summary>
        /// Icon for the template
        /// </summary>
        public required string IconUrl { get; set; }

        /// <summary>
        /// Default domain name prefix
        /// </summary>
        public string? DomainNamePrefix { get; set; }
        public string ApplicationProtocol { get; set; } = "https";

        /// <summary>
        /// Default suggested domain name prefix when adding through UI
        /// </summary>
        public string? DefaultDomainPrefix { get; set; }

        /// <summary>
        /// Textual description of the deployment
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Can be used to group deployments
        /// </summary>
        public string? Category { get; set; } = "Apps";

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
        /// If true, an API port will be allocated
        /// </summary>
        public bool UseApiPort { get; set; }

        /// <summary>
        /// Format depends on kind
        /// for docker compose this is the docker-compose.yaml
        /// </summary>
        public required string Plan { get; set; }

        /// <summary>
        /// Health check tests
        /// </summary>
        public List<DeploymentHealthCheck> HealthChecks { get; set; } = new();

        /// <summary>
        /// Options related to the resource entity created from a deployment
        /// </summary>
        public ResourceCreationOptions Resource { get; set; } = new ResourceCreationOptions();
    }
}
