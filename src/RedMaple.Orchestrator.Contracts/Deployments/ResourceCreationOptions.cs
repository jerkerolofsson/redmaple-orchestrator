using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Deployments
{
    public class ResourceCreationOptions
    {
        public bool Create { get; set; } = true;

        /// <summary>
        /// Environment variables that are exported to the resource from the plan
        /// </summary>
        public Dictionary<string, string> Export { get; set; } = new();
    }
}
