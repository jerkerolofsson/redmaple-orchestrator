using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Healthz
{
    public enum HealthCheckMethod
    {
        HttpGet,
        TcpConnect,
    }
}
