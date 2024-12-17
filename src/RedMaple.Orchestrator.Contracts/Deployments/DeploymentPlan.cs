using Microsoft.Extensions.Diagnostics.HealthChecks;
using RedMaple.Orchestrator.Contracts.Healthz;
using RedMaple.Orchestrator.Contracts.Ingress;
using RedMaple.Orchestrator.Controller.Domain.Healthz.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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

        public string? Description { get; set; }
        public string Category { get; set; } = "Apps";

        /// <summary>
        /// Ingress service created on this domain name (if set)
        /// </summary>
        public string? DomainName { get; set; }

        /// <summary>
        /// If region is set then a DNS entry is added on nodes with the same region. 
        /// This allows multiple deployments with the same domain name
        /// </summary>
        public bool OnlyRegionDnsEntry { get; set; }

        /// <summary>
        /// Region identifier. 
        /// </summary>
        public string? Region { get; set; }

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
        /// Version/Tag. This is read from the deployment plan
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Environment variables that will be set before applying the plan
        /// </summary>
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

        /// <summary>
        /// Binds for volumes in this plan
        /// This only contains user declared volume binds, not the volumes
        /// declared in the docker compose file with both a volume and a service volume bind
        /// </summary>
        public List<VolumeBind> VolumeBinds { get; set; } = new();

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
        /// Application port (secondary port)
        /// </summary>
        public int? ApplicationServerApiPort { get; set; }

        /// <summary>
        /// If enabled an API port will be allocated and the REDMAPLE_API_PORT environment variable will be set
        /// </summary>
        public bool UseApiPort { get; set; } = false;

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
        /// Settings for the reverse proxy
        /// </summary>
        public ReverseProxySettings? ReverseProxy { get; set; }

        /// <summary>
        /// HTTPS certificate for the application server in PFX format
        /// </summary>
        public byte[]? ApplicationHttpsCertificatePfx { get; set; }

        /// <summary>
        /// HTTPS certificate for the application server in PEM format
        /// </summary>
        public byte[]? ApplicationHttpsPemCert { get; set; }

        /// <summary>
        /// HTTPS private key for the application server in PEM format
        /// </summary>
        public byte[]? ApplicationHttpsPemKey { get; set; }


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

        /// <summary>
        /// Timestamp when the health status was changed
        /// </summary>
        public DateTime? HealthStatusChangedTimestamp { get; set; }

        /// <summary>
        /// Timestamp when reliability mitigation was attempted
        /// </summary>
        public DateTime? ReliabiltityMitigationAttemptedTimestamp { get; set; }

        /// <summary>
        /// Options related to the resource entity created from a deployment
        /// </summary>
        public ResourceCreationOptions Resource { get; set; } = new ResourceCreationOptions();

        /// <summary>
        /// This is variables that are assigned from a depdendency (resource)
        /// Key is the environment variable name, and value is the resource id
        /// </summary>
        public Dictionary<string, string> ResourceVariableMap { get; set; } = new();

    }
}
