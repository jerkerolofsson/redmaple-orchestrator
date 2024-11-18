using RedMaple.Orchestrator.Contracts.Healthz;
using RedMaple.Orchestrator.Controller.Domain.Healthz.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Resources
{
    /// <summary>
    /// A resource is a generic abstraction for some kind of service, dns etc.
    /// </summary>
    public class ClusterResource
    {
        public required string Id { get; set; }

        /// <summary>
        /// Name of the resource, descriptive
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Slug of the resource
        /// </summary>
        public required string Slug { get; set; }

        /// <summary>
        /// A global resource is applied to all deployments
        /// </summary>
        public bool IsGlobal { get; set; }

        /// <summary>
        /// Type of resource
        /// </summary>
        public required ResourceKind Kind { get; set; }

        /// <summary>
        /// A persistant resource is saved on disk
        /// 
        /// Deployments, DNS, Ingress are not persistant and their lifetime is controlled by respecive
        /// services.
        /// </summary>
        public bool Persist { get; set; }

        /// <summary>
        /// Environment variable set for the resource
        /// </summary>
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

        /// <summary>
        /// Volume options, specific to a volume
        /// </summary>
        public VolumeResource? Volume { get; set; }

        /// <summary>
        /// Icon. Related to deployment plan
        /// </summary>
        public string? IconUrl { get; set; }
    }
}
