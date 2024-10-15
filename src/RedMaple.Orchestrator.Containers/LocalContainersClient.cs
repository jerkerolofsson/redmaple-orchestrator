using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.Contracts.Containers;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace RedMaple.Orchestrator.Containers
{
    public class LocalContainersClient : ILocalContainersClient
    {
        private const string LABEL_PROJECT = "com.docker.compose.project";

        private ILogger<LocalContainersClient> _logger;

        public LocalContainersClient(ILogger<LocalContainersClient> logger)
        {
            _logger = logger;
        }

        public void Dispose()
        {
        }

        public async Task<NetworksCreateResponse> CreateNetworkAsync(NetworksCreateParameters parameters, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating network {NetworkName}", parameters.Name);
            using var client = new DockerClientConfiguration(new Uri(GetDockerApiUri())).CreateClient();
            return await client.Networks.CreateNetworkAsync(parameters, cancellationToken);
        }

        public async Task<IList<NetworkResponse>> ListNetworksAsync(NetworksListParameters parameters, CancellationToken cancellationToken)
        {
            using var client = new DockerClientConfiguration(new Uri(GetDockerApiUri())).CreateClient();
            return await client.Networks.ListNetworksAsync(parameters, cancellationToken);
        }
        public async Task DeleteNetworkAsync(string id, ContainerRemoveParameters parameters, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting network {NetworkId}", id);
            using var client = new DockerClientConfiguration(new Uri(GetDockerApiUri())).CreateClient();
            await client.Networks.DeleteNetworkAsync(id, cancellationToken);
        }

        public async Task RemoveContainerAsync(string id, ContainerRemoveParameters parameters, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Removing container {ContainerId}", id);
            using var client = new DockerClientConfiguration(new Uri(GetDockerApiUri())).CreateClient();
            await client.Containers.RemoveContainerAsync(id, parameters, cancellationToken);
        }

        public async Task ConnectNetworkAsync(string networkId, 
            NetworkConnectParameters parameters,
            CancellationToken cancellationToken)
        {
            using var client = new DockerClientConfiguration(new Uri(GetDockerApiUri())).CreateClient();
            await client.Networks.ConnectNetworkAsync(networkId, parameters, cancellationToken);
        }
        public async Task<CreateContainerResponse> CreateContainerAsync(CreateContainerParameters parameters, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating container with image {ContainerImage}", parameters.Image);
            using var client = new DockerClientConfiguration(new Uri(GetDockerApiUri())).CreateClient();
            try
            {
                await TryDeleteContainerByNameAsync(parameters.Name);

                var response = await client.Containers.CreateContainerAsync(parameters);
                return response;
            }
            catch (Docker.DotNet.DockerImageNotFoundException)
            {
                string imageName = parameters.Image;
                await PullImageAsync(imageName, new Progress<JSONMessage>(), null,cancellationToken);

                _logger.LogInformation("Creating container with image {ContainerImage}", parameters.Image);
                var response = await client.Containers.CreateContainerAsync(parameters, cancellationToken);
                return response;
            }
        }

        private async Task<bool> TryDeleteContainerByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            var container = await GetContainerByNameAsync(name, cancellationToken);
            if(container is not null)
            {
                await DeleteContainerAsync(container.Id, cancellationToken);
                return true;
            }
            return false;
        }
        public async Task<Container?> GetContainerByIdAsync(string id, CancellationToken cancellationToken)
        {
            var containers = await GetContainersAsync(null, cancellationToken);
            return containers.Where(x => x.Id == id).FirstOrDefault();
        }

        public async Task<Container?> GetContainerByNameAsync(string name, CancellationToken cancellationToken)
        {
            var containers = await GetContainersAsync(null, cancellationToken);
            return containers.Where(x => x.Name == name).FirstOrDefault();
        }
        public async Task<bool> HasContainerByNameAsync(string name, CancellationToken cancellationToken)
        {
            var containers = await GetContainersAsync(null, cancellationToken);
            return containers.Where(x => x.Name == name).Any();
        }

        public async Task<List<Container>> GetContainersAsync(string? project, CancellationToken cancellationToken)
        {
            using var client = new DockerClientConfiguration(new Uri(GetDockerApiUri())).CreateClient();
            var contrainers = await client.Containers.ListContainersAsync(new Docker.DotNet.Models.ContainersListParameters
            {
                All = true
            }, cancellationToken);

            List<Container> response = new();
            foreach (var container in contrainers)
            {
                if(project is not null)
                { 
                    if (!container.Labels.TryGetValue(LABEL_PROJECT, out var containerProject) ||
                        !containerProject.Equals(project, StringComparison.InvariantCultureIgnoreCase))
                    {
                        continue;
                    }
                }
                response.Add(ContainerMapping.Map(container));
            }


            return response;
        }
        private string GetDockerApiUri()
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (isWindows)
            {
                return "npipe://./pipe/docker_engine";
            }
            var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            if (isLinux)
            {
                return "unix:///var/run/docker.sock";
            }

            throw new Exception("Default socket location was not found. Please review your docker socket location");
        }

        public async Task<bool> IsRunningAsync(string containerId, CancellationToken cancellationToken = default)
        {
            using var client = new DockerClientConfiguration(new Uri(GetDockerApiUri())).CreateClient();
            var containers = await client.Containers.ListContainersAsync(new ContainersListParameters()
            {
                All = true
            });
            var container = containers.Where(c => c.ID == containerId).FirstOrDefault();
            if (container is null)
            {
                return false;
            }
            return container.State == "running";
        }

        private class StatsProgressCollector : IProgress<ContainerStatsResponse>
        {
            private IProgress<string> _callback;

            public StatsProgressCollector(IProgress<string> callback)
            {
                _callback = callback;
            }

            public void Report(ContainerStatsResponse value)
            {
                _callback.Report(JsonSerializer.Serialize(value));
            }
        }

        /// <summary>
        /// Removes the header
        /// </summary>
        private class LogsProgressCollector : IProgress<string>
        {
            private IProgress<string> _callback;

            public LogsProgressCollector(IProgress<string> callback)
            {
                _callback = callback;
            }

            public void Report(string value)
            {
                // Docker header
                // header := [8]byte{STREAM_TYPE, 0, 0, 0, SIZE1, SIZE2, SIZE3, SIZE4} 
                var bytes = Encoding.UTF8.GetBytes(value);
                if (bytes.Length >= 8)
                {
                    var lineBytes = bytes[8..];
                    string withoutHeader = DecodeString(lineBytes);
                    _callback.Report(withoutHeader);
                }
                else
                {
                    _callback.Report(value);
                }
            }
        }
        private static string DecodeString(byte[] lineBytes)
        {
            var text = Encoding.UTF8.GetString(lineBytes);
            return text;
        }


        public async Task<VolumeResponse> CreateVolumeAsync(VolumesCreateParameters parameters, CancellationToken cancellationToken)
        {
            using var client = new DockerClientConfiguration(new Uri(GetDockerApiUri())).CreateClient();
            return await client.Volumes.CreateAsync(parameters, cancellationToken);
        }

        public async Task DeleteVolumeAsync(string name, bool force, CancellationToken cancellationToken)
        {
            using var client = new DockerClientConfiguration(new Uri(GetDockerApiUri())).CreateClient();
            await client.Volumes.RemoveAsync(name, force, cancellationToken);
        }

        public async Task<VolumesListResponse> ListVolumesAsync(CancellationToken cancellationToken)
        {
            using var client = new DockerClientConfiguration(new Uri(GetDockerApiUri())).CreateClient();
            return await client.Volumes.ListAsync(new VolumesListParameters { }, cancellationToken);
        }



        public async Task PullImageAsync(string imageName, IProgress<JSONMessage> progress,
            CancellationTokenSource? cts,
            CancellationToken cancellationToken)
        {
            using var client = new DockerClientConfiguration(new Uri(GetDockerApiUri())).CreateClient();
            _logger.LogInformation("Pulling image {ImageName}", imageName);
            var createParameters = new ImagesCreateParameters
            {
                FromImage = imageName
            };
            await client.Images.CreateImageAsync(createParameters, null, progress, cancellationToken);
            cts?.Cancel();
        }
        public Task GetLogsAsync(string id, 
            bool follow,
            string? tail,
            IProgress<string> callback, 
            CancellationTokenSource cts,
            CancellationToken cancellationToken)
        {
            var _ = Task.Factory.StartNew(async (object? state) =>
            {
                try
                {
                    using var client = new DockerClientConfiguration(new Uri(GetDockerApiUri())).CreateClient();
                    LogsProgressCollector progress = new(callback);
                    await client.Containers.GetContainerLogsAsync(id, new ContainerLogsParameters
                    {
                        ShowStdout = true,
                        ShowStderr = true,
                        Follow = follow,
                        Tail = tail
                    }, cancellationToken, progress);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "GetContainerLogsAsync stopped for container {ContainerId}", id);
                }
                finally
                {
                    cts.Cancel();
                }
            }, null, TaskCreationOptions.LongRunning);
            return Task.CompletedTask;
        }
        /// <summary>
        /// Callback will receive jsonl
        /// </summary>
        /// <param name="id"></param>
        /// <param name="callback"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>

        public Task GetStatsAsync(
            string id,
            IProgress<string> callback,
            CancellationTokenSource cts,
            CancellationToken cancellationToken)
        {
            var _ = Task.Factory.StartNew(async (object? state) =>
            {
                try
                {
                    using var client = new DockerClientConfiguration(new Uri(GetDockerApiUri())).CreateClient();
                    StatsProgressCollector progress = new(callback);
                    await client.Containers.GetContainerStatsAsync(id, new ContainerStatsParameters
                    {
                    }, progress, cancellationToken);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "GetStatsAsync stopped for container {ContainerId}", id);
                }
                finally
                {
                    cts.Cancel();
                }
            }, null, TaskCreationOptions.LongRunning);
            return Task.CompletedTask;
        }
        public async Task StopAsync(string id, CancellationToken cancellationToken)
        {
            using var client = new DockerClientConfiguration(new Uri(GetDockerApiUri())).CreateClient();
            await client.Containers.StopContainerAsync(id, new ContainerStopParameters
            {
            }, cancellationToken);

            // Wait for the container to s top
            while(!cancellationToken.IsCancellationRequested)
            {
                var isRunning = await IsRunningAsync(id, cancellationToken);
                if(!isRunning)
                {
                    break;
                }
            }
        }
        public async Task StartAsync(string id, CancellationToken cancellationToken)
        {
            using var client = new DockerClientConfiguration(new Uri(GetDockerApiUri())).CreateClient();
            await client.Containers.StartContainerAsync(id, new ContainerStartParameters
            {
            }, cancellationToken);
        }

        public async Task RestartAsync(string id, CancellationToken cancellationToken)
        {
            using var client = new DockerClientConfiguration(new Uri(GetDockerApiUri())).CreateClient();
            await client.Containers.RestartContainerAsync(id, new ContainerRestartParameters
            {
                WaitBeforeKillSeconds = 30
            }, cancellationToken);
        }

        public async Task DeleteContainerAsync(string id, CancellationToken cancellationToken)
        {
            using var client = new DockerClientConfiguration(new Uri(GetDockerApiUri())).CreateClient();
            await client.Containers.RemoveContainerAsync(id, new ContainerRemoveParameters
            {
                Force = true
            }, cancellationToken);
        }
    }
}
