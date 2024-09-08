using RedMaple.Orchestrator.Containers;
using RedMaple.Orchestrator.Contracts.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ContainersServiceExtensions
    {
        public static IServiceCollection AddContainerServices(this IServiceCollection services)
        {
            services.AddTransient<ILocalContainersClient, LocalContainersClient>();
            return services;
        }
    }
}
