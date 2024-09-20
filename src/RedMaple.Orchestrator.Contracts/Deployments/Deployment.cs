using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Deployments
{
    public record class Deployment(
        string Id,
        string Slug,
        ResourceCreationOptions Resource,
        string ApplicationServerIp, 
        int? ApplicationServerPort,
        string? ApplicationProtocol,
        Dictionary<string,string> EnvironmentVariables);
}
