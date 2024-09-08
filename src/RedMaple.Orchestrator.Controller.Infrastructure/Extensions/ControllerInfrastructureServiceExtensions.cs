using RedMaple.Orchestrator.Contracts.Ingress;
using RedMaple.Orchestrator.Contracts.Node;
using RedMaple.Orchestrator.Controller.Infrastructure.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ControllerInfrastructureServiceExtensions
    {
        public static IServiceCollection AddControllerInfrastructure(this IServiceCollection services)
        {
            services.AddSingleton<INodeRepository, InMemoryNodeRepository>();
            services.AddSingleton<IIngressRepository, FileSystemIngressRepository>();
            return services;
        }
    }
}
