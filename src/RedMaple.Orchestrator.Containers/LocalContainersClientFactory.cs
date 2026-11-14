using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.Extensions.DependencyInjection;

using RedMaple.Orchestrator.Contracts.Containers;

namespace RedMaple.Orchestrator.Containers
{
    internal class LocalContainersClientFactory : ILocalContainersClientFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public LocalContainersClientFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Backend to use
        /// </summary>
        private ContainerBackend _backend = ContainerBackend.Docker;

        /// <summary>
        /// Creates a container client based on the configured backend
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public ILocalContainersClient Create()
        {
            return Create(_backend);
        }

        /// <summary>
        /// Creates a container client based on the specified backend
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public ILocalContainersClient Create(ContainerBackend backend)
        {
            if(backend == ContainerBackend.WslContainers)
            {
                var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                if(!isWindows)
                {
                    throw new NotSupportedException("WSL Containers are only supported on Windows.");
                }
            }

            return backend switch
            {
                ContainerBackend.Docker => _serviceProvider.GetRequiredService<LocalContainersClient>(),
                ContainerBackend.Podman => _serviceProvider.GetRequiredService<PodmanContainerClient>(),
                ContainerBackend.WslContainers => _serviceProvider.GetRequiredService<IWslcClient>(),
                _ => throw new NotSupportedException()
            };
        }

        public void SetBackend(ContainerBackend backend)
        {
            _backend = backend;
        }

        public ContainerBackend GetBackend()
        {
            return _backend;
        }
    }
}
