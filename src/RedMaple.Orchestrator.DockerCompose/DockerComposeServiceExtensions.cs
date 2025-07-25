using Microsoft.Extensions.DependencyInjection;

using RedMaple.Orchestrator.DockerCompose;

namespace Microsoft.DependencyInject.Extensions
{
    public static class DockerComposeServiceExtensions
    {
        public static IServiceCollection AddDockerCompose(this IServiceCollection services)
        {
            services.AddContainerServices();
            services.AddTransient<IDockerCompose, DockerCompose>();
            services.AddTransient<IDockerVolumeManager, DockerVolumeManager>();
            return services;
        }
    }
}
