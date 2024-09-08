using RedMaple.Orchestrator.Containers;
using RedMaple.Orchestrator.Ingress;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IngressServiceExtensions
    {
        public static IServiceCollection AddIngress(this IServiceCollection services)
        {
            services.AddTransient<IReverseProxy, Nginx>();
            services.AddTransient<LocalIngressServiceController>();
            return services;
        }
    }
}
