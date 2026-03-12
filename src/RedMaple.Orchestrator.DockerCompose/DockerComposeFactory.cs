using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Extensions.DependencyInjection;

namespace RedMaple.Orchestrator.DockerCompose
{
    /// <summary>
    /// Main entry to use docker compose 
    /// </summary>
    public static class DockerComposeFactory
    {
        private static IServiceProvider CreateServiceProvider()
        {
            ServiceCollection services = new();
            RegisterDockerComposeServices(services);
            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Registers required services.
        /// </summary>
        /// <param name="services"></param>
        private static void RegisterDockerComposeServices(ServiceCollection services)
        {
            services.AddLogging();
            services.AddDockerCompose();
        }

        /// <summary>
        /// Creates an instance of the DockerCompose class with all dependencies resolved and returns it
        /// </summary>
        /// <returns></returns>
        public static IDockerCompose Create()
        {
            var serviceProvider = CreateServiceProvider();
            return serviceProvider.GetRequiredService<IDockerCompose>();
        }
    }
}
