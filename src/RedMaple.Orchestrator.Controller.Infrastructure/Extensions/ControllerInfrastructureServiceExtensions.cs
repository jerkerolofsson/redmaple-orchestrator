﻿using RedMaple.Orchestrator.Contracts.Deployments;
using RedMaple.Orchestrator.Contracts.Dns;
using RedMaple.Orchestrator.Contracts.Infra;
using RedMaple.Orchestrator.Contracts.Ingress;
using RedMaple.Orchestrator.Contracts.Node;
using RedMaple.Orchestrator.Contracts.Resources;
using RedMaple.Orchestrator.Contracts.Secrets;
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
            services.AddSingleton<IGlobalDnsRepository, GlobalDnsRepository>();
            services.AddSingleton<INodeRepository, InMemoryNodeRepository>();
            services.AddSingleton<IIngressRepository, FileSystemIngressRepository>();
            services.AddSingleton<IApplicationDeploymentRepository, ApplicationDeploymentRepository>();
            services.AddSingleton<IDeploymentPlanRepository, DeploymentPlanRepository>();
            services.AddSingleton<IDeploymentPlanTemplateRepository, DeploymentPlanTemplateRepository>();
            services.AddSingleton<IClusterResourceRepository, ClusterResourceRepository>();
            services.AddSingleton<ISecretsRepository, SecretsRepository>();
            services.AddSingleton<IInfraRepository, InfraRepository>();

            return services;
        }
    }
}
