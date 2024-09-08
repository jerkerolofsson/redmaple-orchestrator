using RedMaple.Orchestrator.Contracts;
using RedMaple.Orchestrator.Contracts.Ingress;
using RedMaple.Orchestrator.Controller.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ControllerDomainServiceExtensions
    {
        public static IServiceCollection AddControllerDomainServices(this IServiceCollection services)
        {
            services.AddSingleton<IIngressManager, IngressManager>();
            services.AddSingleton<INodeManager, NodeManager>();
            return services;
        }
    }
}
