using RedMaple.Orchestrator.Contracts;
using RedMaple.Orchestrator.Contracts.Ingress;
using RedMaple.Orchestrator.Controller.Domain.Deployments;
using RedMaple.Orchestrator.Controller.Domain.Deployments.Templates;
using RedMaple.Orchestrator.Controller.Domain.Domain;
using RedMaple.Orchestrator.Controller.Domain.GlobalDns;
using RedMaple.Orchestrator.Controller.Domain.Healthz;
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
            services.AddSingleton<IDomainService, DomainService>();
            services.AddSingleton<IDeploymentManager, DeploymentManager>();
            services.AddSingleton<IHealthzChecker, HealthzChecker>();
            
            services.AddHostedService<PeriodicDeploymentHealthzChecker>();

            services.AddSingleton<INodeManager, NodeManager>();
            services.AddSingleton<IDeploymentTemplateProvider, DeploymentTemplateProvider>();
            return services;
        }
    }
}
