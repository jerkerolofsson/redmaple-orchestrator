using Docker.DotNet;
using Docker.DotNet.Models;
using RedMaple.Orchestrator.Contracts.Containers;
using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;

namespace RedMaple.Orchestrator.Containers
{
    public class LocalContainersClient : ILocalContainersClient
    {
        public void Dispose()
        {
        }

        public async Task RemoveContainerAsync(string id, ContainerRemoveParameters parameters, CancellationToken cancellationToken)
        {
            using var client = new DockerClientConfiguration(new Uri(GetDockerApiUri())).CreateClient();
            await client.Containers.RemoveContainerAsync(id, parameters);
        }
        public async Task<CreateContainerResponse> CreateContainerAsync(CreateContainerParameters parameters, CancellationToken cancellationToken)
        {
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

                var response = await client.Containers.CreateContainerAsync(parameters, cancellationToken);
                return response;
            }
        }

        private static async Task PullImageAsync(DockerClient client, string imageName, CancellationToken cancellationToken)
        {
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
    }
}
