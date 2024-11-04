using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Domain.Healthz.Models
{
    public class ResourceHealthCheckResult
    {
        /// <summary>
        /// Duration for the the health check
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Current status
        /// </summary>
        public HealthStatus Status { get; set; }
    }
}
