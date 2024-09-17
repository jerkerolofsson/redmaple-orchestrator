using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Deployments
{
    public record class ApplicationDeployment(
        string Id,
        string Slug, 
        string ApplicationServerIp, 
        int? ApplicationServerPort,
        string? ApplicationProtocol);
}
