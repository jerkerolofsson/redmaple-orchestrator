using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Domain.Metrics.Models
{
    public class ContainerStats
    {
        public required string ContainerId { get; set; }

        /// <summary>
        /// CPU Usage, 0.0 - 100.0
        /// </summary>
        public double CpuUsage { get; set; }
    }
}
