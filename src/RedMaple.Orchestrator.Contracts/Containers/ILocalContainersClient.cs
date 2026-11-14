using Docker.DotNet;
using Docker.DotNet.Models;

namespace RedMaple.Orchestrator.Contracts.Containers
{
    /// <summary>
    /// Work with containers
    /// </summary>
    public interface ILocalContainersClient : IDisposable
    {
        /// <summary>
        /// Login to a repository
        /// </summary>
        /// <param name="registry"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task LoginAsync(string registry, string username, string password, CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts a container by its ID.
        /// </summary>
        /// <param name="id">The container ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task StartAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops a container by its ID.
        /// </summary>
        /// <param name="id">The container ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task StopAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new container.
        /// </summary>
        /// <param name="parameters">The container creation parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response containing the created container information.</returns>
        Task<CreateContainerResponse> CreateContainerAsync(CreateContainerParameters parameters, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a container by its ID.
        /// </summary>
        /// <param name="id">The container ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The container if found; otherwise, null.</returns>
        Task<Container?> GetContainerByIdAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a container by its name.
        /// </summary>
        /// <param name="name">The container name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The container if found; otherwise, null.</returns>
        Task<Container?> GetContainerByNameAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a list of containers, optionally filtered by project.
        /// </summary>
        /// <param name="project">The project filter, or null for no filtering.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of containers.</returns>
        Task<List<Container>> GetContainersAsync(string? project, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a container with the specified name exists.
        /// </summary>
        /// <param name="name">The container name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if a container with the name exists; otherwise, false.</returns>
        Task<bool> HasContainerByNameAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets logs from a container, optionally following new log entries.
        /// </summary>
        /// <param name="id">The container ID.</param>
        /// <param name="follow">Whether to follow new log entries as they are generated.</param>
        /// <param name="tail">The number of log lines to return from the end, or null for all lines.</param>
        /// <param name="callback">The progress callback that receives log lines as strings.</param>
        /// <param name="cts">The cancellation token source that will be cancelled when there are no more callbacks.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task GetLogsAsync(
            string id, 
            bool follow,
            string? tail,
            IProgress<string> callback,
            CancellationTokenSource cts, 
            CancellationToken cancellationToken);

        /// <summary>
        /// Starts reading stats from a container, invoking the callback with each response as a JSON lines (jsonl) string.
        /// </summary>
        /// <param name="id">The container ID.</param>
        /// <param name="callback">The progress callback that receives stats as jsonl strings.</param>
        /// <param name="cts">The cancellation token source that will be cancelled when there are no more callbacks.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task GetStatsAsync(
            string id, 
            IProgress<string> callback,
            CancellationTokenSource cts,
            CancellationToken cancellationToken);

        /// <summary>
        /// Creates a new Docker network.
        /// </summary>
        /// <param name="parameters">The network creation parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response containing the created network information.</returns>
        Task<NetworksCreateResponse> CreateNetworkAsync(NetworksCreateParameters parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a Docker network.
        /// </summary>
        /// <param name="id">The network ID.</param>
        /// <param name="parameters">The network deletion parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteNetworkAsync(string id, ContainerRemoveParameters parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Lists Docker networks based on the provided filters.
        /// </summary>
        /// <param name="parameters">The network list parameters with optional filters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of networks matching the filters.</returns>
        Task<IList<NetworkResponse>> ListNetworksAsync(NetworksListParameters parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a container by its ID.
        /// </summary>
        /// <param name="id">The container ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteContainerAsync(string id, CancellationToken cancellationToken);

        /// <summary>
        /// Connects a container to a network.
        /// </summary>
        /// <param name="networkId">The network ID.</param>
        /// <param name="parameters">The network connection parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task ConnectNetworkAsync(string networkId, NetworkConnectParameters parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Lists all Docker volumes.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response containing the list of volumes.</returns>
        Task<VolumesListResponse> ListVolumesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a Docker volume.
        /// </summary>
        /// <param name="name">The volume name.</param>
        /// <param name="force">Whether to force the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteVolumeAsync(string name, bool force, CancellationToken cancellationToken);

        /// <summary>
        /// Creates a new Docker volume.
        /// </summary>
        /// <param name="parameters">The volume creation parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response containing the created volume information.</returns>
        Task<VolumeResponse> CreateVolumeAsync(VolumesCreateParameters parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Pulls a Docker image from a registry.
        /// </summary>
        /// <param name="imageName">The name of the image to pull.</param>
        /// <param name="progress">The progress callback for pull progress updates.</param>
        /// <param name="cts">The cancellation token source, or null if not needed.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task PullImageAsync(string imageName, IProgress<JSONMessage> progress, CancellationTokenSource? cts,
            CancellationToken cancellationToken);

        /// <summary>
        /// Restarts a container.
        /// </summary>
        /// <param name="id">The container ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task RestartAsync(string id, CancellationToken cancellationToken);

        /// <summary>
        /// Executes commands inside a running container.
        /// </summary>
        /// <param name="containerId">The container ID.</param>
        /// <param name="commands">The list of commands to execute.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task ExecAsync(string containerId, IList<string> commands, CancellationToken cancellationToken);

        /// <summary>
        /// Removes a Docker volume.
        /// </summary>
        /// <param name="name">The volume name.</param>
        /// <param name="force">Whether to force the removal.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task RemoveVolumeAsync(string name, bool force, CancellationToken cancellationToken);

        /// <summary>
        /// Checks if a Docker image exists.
        /// </summary>
        /// <param name="image">The image name or ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the image exists; otherwise, false.</returns>
        Task<bool> HasImageAsync(string image, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the ID of a Docker image.
        /// </summary>
        /// <param name="image">The image name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The image ID if found; otherwise, null.</returns>
        Task<string?> GetImageIdAsync(string image, CancellationToken cancellationToken);

        /// <summary>
        /// Inspects a Docker image and returns detailed information.
        /// </summary>
        /// <param name="image">The image name or ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The detailed image information.</returns>
        Task<ImageInspectResponse?> InspectImageAsync(string image, CancellationToken cancellationToken);

        /// <summary>
        /// Lists all Docker images.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of images.</returns>
        Task<IList<ImagesListResponse>> ListImagesAsync(CancellationToken cancellationToken);
    }
}