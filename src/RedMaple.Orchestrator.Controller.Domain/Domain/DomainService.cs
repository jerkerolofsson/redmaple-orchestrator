using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Domain.Domain
{
    internal class DomainService : IDomainService
    {
        public string DefaultDomain => Environment.GetEnvironmentVariable("REDMAPLE_DEFAULT_DOMAIN") ?? "home.io";
    }
}
