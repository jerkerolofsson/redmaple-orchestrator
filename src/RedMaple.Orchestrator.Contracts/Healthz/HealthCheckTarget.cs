using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Healthz
{
    public enum HealthCheckTarget
    {
        /// <summary>
        /// Health check will be performed against the ingress
        /// </summary>
        Ingress,

        /// <summary>
        /// Health check will be performed against the load balancer
        /// </summary>
        LoadBalancer,

        /// <summary>
        /// Health check will be performed against the application
        /// </summary>
        Application
    }
}
