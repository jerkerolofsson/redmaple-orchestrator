using Docker.DotNet;
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
        Task<Container?> GetContainerByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<Container?> GetContainerByNameAsync(string name, CancellationToken cancellationToken = default);
        Task<List<Container>> GetContainersAsync(string? project, CancellationToken cancellationToken = default);
        Task<bool> HasContainerByNameAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="follow"></param>
        /// <param name="callback"></param>
        /// <param name="cts">Will be cancelled when there are no more callbacks</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task GetLogsAsync(
            string id, 
            bool follow,
            string? tail,
            IProgress<string> callback,
            CancellationTokenSource cts, 
            CancellationToken cancellationToken);

        /// <summary>
        /// Starts reading stats from the container, invoking the callback with each response (as a jsonl string)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="callback"></param>
        /// <param name="cts">Will be cancelled when there are no more callbacks</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task GetStatsAsync(
            string id, 
            IProgress<string> callback,
            CancellationTokenSource cts,
            CancellationToken cancellationToken);
        Task<NetworksCreateResponse> CreateNetworkAsync(NetworksCreateParameters parameters, CancellationToken cancellationToken);
        Task DeleteNetworkAsync(string id, ContainerRemoveParameters parameters, CancellationToken cancellationToken);
        Task<IList<NetworkResponse>> ListNetworksAsync(NetworksListParameters parameters, CancellationToken cancellationToken);
        Task DeleteContainerAsync(string id, CancellationToken cancellationToken);
        Task ConnectNetworkAsync(string networkId, NetworkConnectParameters parameters, CancellationToken cancellationToken);
        Task<VolumesListResponse> ListVolumesAsync(CancellationToken cancellationToken);
        Task DeleteVolumeAsync(string name, bool force, CancellationToken cancellationToken);
        Task<VolumeResponse> CreateVolumeAsync(VolumesCreateParameters parameters, CancellationToken cancellationToken);
        Task PullImageAsync(string imageName, IProgress<JSONMessage> progress, CancellationTokenSource? cts,
            CancellationToken cancellationToken);
        Task RestartAsync(string id, CancellationToken cancellationToken);
        Task ExecAsync(string containerId, IList<string> commands, CancellationToken cancellationToken);
    }
}