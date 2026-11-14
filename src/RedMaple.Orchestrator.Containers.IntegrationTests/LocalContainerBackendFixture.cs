using Microsoft.Extensions.DependencyInjection;

using RedMaple.Orchestrator.Containers;
using RedMaple.Orchestrator.Containers.Wslc;
using RedMaple.Orchestrator.Contracts.Containers;

namespace RedMaple.Orchestration.Containers.IntegrationTests
{
    internal class LocalContainerBackendFixture : IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ContainerBackend _backend;

        /// <summary>
        /// Gets the local container client based on the specified backend
        /// </summary>
        public ILocalContainersClient LocalContainersClient => _serviceProvider.GetRequiredService<ILocalContainersClientFactory>().Create(_backend);

        public LocalContainerBackendFixture(ContainerBackend backend)
        {
            ServiceCollection services = new ServiceCollection();
            services.AddContainerServices();
            services.AddLogging();
            services.AddTransient<IWslcClient, WslcClient>();
            _serviceProvider = services.BuildServiceProvider();
            _backend = backend;
        }

        public void Dispose()
        {
            if(_serviceProvider is ServiceProvider serviceProvider)
            {
                serviceProvider.Dispose();
            }
        }
    }
}
