using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RedMaple.Orchestrator.Contracts.Resources;

namespace RedMaple.Orchestrator.Contracts.Deployments
{
    public class ResourceCreationOptions
    {
        /// <summary>
        /// If null, deployment plan will be used
        /// </summary>
        public string? ServiceName { get; set; }

        /// <summary>
        /// Type of resource
        /// </summary>
        public ResourceKind? Kind { get; set; } = ResourceKind.ApplicationService;

        /// <summary>
        /// True to create a resource
        /// </summary>
        public bool Create { get; set; } = true;

        /// <summary>
        /// If not null, a connection string is created from the format specified
        /// </summary>
        public string? ConnectionStringVariableNameFormat { get; set; }

        /// <summary>
        /// If not null, a connection string environment variable is created based on the format
        /// </summary>
        public string? ConnectionStringFormat { get; set; }


        /// <summary>
        /// Environment variables that are exported to the resource from the plan
        /// </summary>
        public List<string> Exported { get; set; } = new();
    }
}
