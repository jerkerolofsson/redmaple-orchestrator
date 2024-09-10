using Docker.DotNet.Models;

namespace RedMaple.Orchestrator.Contracts.Containers
{
    /// <summary>
    /// Work with containers
    /// </summary>
    public interface ILocalContainersClient : IDisposable
    {
        Task StartAsync(string id, CancellationToken cancellationToken = default);
        Task StopAsync(string id, CancellationToken cancellationToken = default);
        Task<CreateContainerResponse> CreateContainerAsync(CreateContainerParameters parameters, CancellationToken cancellationToken = default);
        Task<Container?> GetContainerByNameAsync(string name, CancellationToken cancellationToken = default);
        Task<List<Container>> GetContainersAsync(CancellationToken cancellationToken = default);
        Task<bool> HasContainerByNameAsync(string name, CancellationToken cancellationToken = default);
    }
}