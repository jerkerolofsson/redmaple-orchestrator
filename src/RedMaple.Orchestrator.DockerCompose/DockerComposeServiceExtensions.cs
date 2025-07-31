using RedMaple.Orchestrator.DockerCompose;

namespace Microsoft.Extensions.DependencyInjection
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
