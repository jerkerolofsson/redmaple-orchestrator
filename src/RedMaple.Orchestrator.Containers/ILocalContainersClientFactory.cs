using RedMaple.Orchestrator.Contracts.Containers;

namespace RedMaple.Orchestrator.Containers
{
    public interface ILocalContainersClientFactory
    {
        /// <summary>
        /// Sets the backend to use (docker, podman, wslc..)
        /// </summary>
        /// <param name="backend"></param>
        void SetBackend(ContainerBackend backend);

        /// <summary>
        /// Creates a container client based on the configured backend
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        ILocalContainersClient Create();

        /// <summary>
        /// Creates a container client based on the specified backend
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        ILocalContainersClient Create(ContainerBackend backend);
        ContainerBackend GetBackend();
    }
}
