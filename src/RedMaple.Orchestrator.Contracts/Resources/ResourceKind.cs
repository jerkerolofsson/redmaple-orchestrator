using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Resources
{
    public enum ResourceKind
    {
        UserDefined,
        OidcServer,
        OtlpServer,
        MessageBroker,
        LargeLanguageModel,
        API,

        Volume,

        ConnectionString,

        ApplicationService,
        Ingress,
        Dns,
        LoadBalancer
    }
}
