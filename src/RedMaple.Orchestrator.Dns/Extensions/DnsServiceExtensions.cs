using RedMaple.Orchestrator.Containers;
using RedMaple.Orchestrator.Contracts.Dns;
using RedMaple.Orchestrator.Dns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DnsServiceExtensions
    {
        public static IServiceCollection AddDns(this IServiceCollection services)
        {
            services.AddSingleton<IDns, DnsMasq>();
            return services;
        }
    }
}
