using Docker.DotNet.Models;
using RedMaple.Orchestrator.Contracts;

namespace RedMaple.Orchestrator.Containers
{
    /// <summary>
    /// Work with containers
    /// </summary>
    public interface ILocalContainersClient : IDisposable
    {
        Task StartAsync(string id);
        Task StopAsync(string id);
        Task<CreateContainerResponse> CreateContainerAsync(CreateContainerParameters parameters);
        Task<Container?> GetContainerByNameAsync(string name);
        Task<List<Container>> GetContainersAsync();
        Task<bool> HasContainerByNameAsync(string name);
    }
}