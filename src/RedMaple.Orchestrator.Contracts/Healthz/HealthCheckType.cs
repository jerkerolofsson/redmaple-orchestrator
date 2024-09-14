using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Healthz
{
    public enum HealthCheckType
    {

        /// <summary>
        /// Application is alive
        /// </summary>
        Livez,

        /// <summary>
        /// Application is ready
        /// </summary>
        Readyz,
    }
}
