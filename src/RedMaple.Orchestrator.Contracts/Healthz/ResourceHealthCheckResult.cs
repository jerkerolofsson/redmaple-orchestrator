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
        public TimeSpan Duration { get; set; }
        public HealthStatus Status { get; set; }
    }
}
