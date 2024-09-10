using RedMaple.Orchestrator.Contracts;
using RedMaple.Orchestrator.Contracts.Ingress;
using RedMaple.Orchestrator.Controller.Domain.GlobalDns;
using RedMaple.Orchestrator.Controller.Domain.Ingress;
using RedMaple.Orchestrator.Controller.Domain.Node;
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
            services.AddMediatR(options =>
            {
                options.RegisterServicesFromAssembly(typeof(GlobalDnsService).Assembly);
            });
            services.AddSingleton<IGlobalDns, GlobalDnsService>();
            services.AddSingleton<IIngressManager, IngressManager>();
            services.AddSingleton<INodeManager, NodeManager>();
            return services;
        }
    }
}
