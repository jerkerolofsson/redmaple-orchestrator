using RedMaple.Orchestrator.Containers;
using RedMaple.Orchestrator.Contracts.Containers;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ContainersServiceExtensions
    {
        /// <summary>
        /// Adds local container clients to the service collection
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddContainerServices(this IServiceCollection services)
        {
            services.AddTransient<ILocalContainersClient, LocalContainersClient>();
            services.AddTransient<LocalContainersClient>();
            services.AddTransient<PodmanContainerClient>();
            services.AddSingleton<ILocalContainersClientFactory, LocalContainersClientFactory>();
            return services;
        }
    }
}
