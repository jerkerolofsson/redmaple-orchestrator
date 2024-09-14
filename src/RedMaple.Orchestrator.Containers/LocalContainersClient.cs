using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.Contracts.Containers;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace RedMaple.Orchestrator.Containers
{
    public class LocalContainersClient : ILocalContainersClient
    {
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
        public async Task<CreateContainerResponse> CreateContainerAsync(CreateContainerParameters parameters, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating container with image {ContainerImage}", parameters.Image);
            using var client = new DockerClientConfiguration(new Uri(GetDockerApiUri())).CreateClient();
            try
            {
                var response = await client.Containers.CreateContainerAsync(parameters);
                return response;
            }
            catch (Docker.DotNet.DockerImageNotFoundException)
            {
                string imageName = parameters.Image;
                await PullImageAsync(client, imageName, cancellationToken);

                _logger.LogInformation("Creating container with image {ContainerImage}", parameters.Image);
                var response = await client.Containers.CreateContainerAsync(parameters, cancellationToken);
                return response;
            }
        }

        private async Task PullImageAsync(DockerClient client, string imageName, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Pulling image {ContainerImage}", imageName);
            var createParameters = new ImagesCreateParameters
            {
                FromImage = imageName
            };
            await client.Images.CreateImageAsync(createParameters, null, new Progress<JSONMessage>(), cancellationToken);
        }

        public async Task<Container?> GetContainerByNameAsync(string name, CancellationToken cancellationToken)
        {
            var containers = await GetContainersAsync(cancellationToken);
            return containers.Where(x => x.Name == name).FirstOrDefault();
        }
        public async Task<bool> HasContainerByNameAsync(string name, CancellationToken cancellationToken)
        {
            var containers = await GetContainersAsync(cancellationToken);
            return containers.Where(x => x.Name == name).Any();
        }

        public async Task<List<Container>> GetContainersAsync(CancellationToken cancellationToken)
        {
            using var client = new DockerClientConfiguration(new Uri(GetDockerApiUri())).CreateClient();
            var contrainers = await client.Containers.ListContainersAsync(new Docker.DotNet.Models.ContainersListParameters
            {
                All = true
            }, cancellationToken);

            List<Container> response = new();
            foreach (var container in contrainers)
            {
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

        private class LogsProgressCollector : IProgress<string>
        {
            public List<string> Lines { get; } = new();

            public void Report(string value)
            {
                Lines.Add(value);
            }
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
                    LogsProgressCollector progress = new();
                    await client.Containers.GetContainerLogsAsync(id, new ContainerLogsParameters
                    {
                        ShowStdout = true,
                        ShowStderr = true,
                        Follow = follow,
                        Tail = tail
                    }, cancellationToken, callback);
                }
                catch(Exception ex)
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
