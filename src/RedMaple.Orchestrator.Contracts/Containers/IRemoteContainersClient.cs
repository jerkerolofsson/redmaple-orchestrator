using Docker.DotNet.Models;

namespace RedMaple.Orchestrator.Contracts.Containers
{
    /// <summary>
    /// Work with containers
    /// </summary>
    public interface IRemoteContainersClient : IDisposable
    {
        Task<CreateContainerResponse> CreateContainerAsync(CreateContainerParameters parameters);
        Task<List<Container>> GetContainersAsync();
        Task RestartAsync(string id);
        Task StartAsync(string id);
        Task StopAsync(string id);
    }
}