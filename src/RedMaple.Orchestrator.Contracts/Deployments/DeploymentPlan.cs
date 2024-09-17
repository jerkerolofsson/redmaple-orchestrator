using Microsoft.Extensions.Diagnostics.HealthChecks;
using RedMaple.Orchestrator.Contracts.Healthz;
using RedMaple.Orchestrator.Controller.Domain.Healthz.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Deployments
{
    public class DeploymentPlan
    {
        public required string Id { get; set; }

        public string? IconUrl { get; set; }

        /// <summary>
        /// Slug generated from the name
        /// </summary>
        public required string Slug { get; set; }

        /// <summary>
        /// Name of the deployment
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Ingress service created on this domain name (if set)
        /// </summary>
        public string? DomainName { get; set; }

        /// <summary>
        /// Kind of deployment
        /// </summary>
        public DeploymentKind Kind { get; set; } = DeploymentKind.DockerCompose;

        /// <summary>
        /// Format depends on kind
        /// for docker compose this is the docker-compose.yaml
        /// </summary>
        public required string Plan { get; set; }

        /// <summary>
        /// Environment variables that will be set before applying the plan
        /// </summary>
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

        /// <summary>
        /// IP address of node that will host the service
        /// </summary>
        //public string? ApplicationServerIp { get; set; }

        public List<string> ApplicationServerIps { get; set; } = new();

        /// <summary>
        /// Application protocol
        /// </summary>
        public string ApplicationProtocol { get; set; } = "https";

        /// <summary>
        /// Application port
        /// </summary>
        public int? ApplicationServerPort { get; set; }

        /// <summary>
        /// IP address of node that will host the ingress service
        /// </summary>
        public string? IngressServerIp { get; set; }

        /// <summary>
        /// If true, a DNS entry will be created
        /// If the deployment uses an ingress server, the DNS entry will point to the ingress server, otherwise to the deployment
        /// </summary>
        public bool CreateDnsEntry { get; set; }

        /// <summary>
        /// If true, ingress server will be created
        /// </summary>
        public bool CreateIngress { get; set; }

        /// <summary>
        /// HTTPS certificate for the application server
        /// </summary>
        public byte[]? ApplicationHttpsCertificatePfx { get; set; }

        /// <summary>
        /// Password for the PFX certificate
        /// </summary>
        public string? ApplicationHttpsCertificatePassword { get; set; }

        /// <summary>
        /// List of container images the plan has.
        /// This is extracted from the plan (e.g. docker compose)
        /// </summary>
        public List<string> ContainerImages { get; set; } = new();

        /// <summary>
        /// Flag to indicate if the deployment is up or down
        /// </summary>
        public bool Up { get; set; }

        /// <summary>
        /// Health check tests
        /// </summary>
        public List<DeploymentHealthCheck> HealthChecks { get; set; } = new();

        /// <summary>
        /// Status for health check
        /// </summary>
        public ResourceHealthCheckResult? Health { get; set; }
    }
}
