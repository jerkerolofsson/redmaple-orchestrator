using Docker.DotNet;
using Docker.DotNet.Models;
using RedMaple.Orchestrator.Containers.Mapping;
using RedMaple.Orchestrator.Contracts;
using System.Runtime.InteropServices;

namespace RedMaple.Orchestrator.Containers
{
    public class LocalContainersClient : ILocalContainersClient
    {
        public void Dispose()
        {
        }

        public async Task RemoveContainerAsync(string id, ContainerRemoveParameters parameters)
        {
            using var client = new DockerClientConfiguration(new Uri(GetDockerApiUri())).CreateClient();
            await client.Containers.RemoveContainerAsync(id, parameters);
        }
        public async Task<CreateContainerResponse> CreateContainerAsync(CreateContainerParameters parameters)
        {
            using var client = new DockerClientConfiguration(new Uri(GetDockerApiUri())).CreateClient();
            var response = await client.Containers.CreateContainerAsync(parameters);
            return response;
        }

        public async Task<Container?> GetContainerByNameAsync(string name)
        {
            var containers = await GetContainersAsync();
            return containers.Where(x => x.Name == name).FirstOrDefault();
        }
        public async Task<bool> HasContainerByNameAsync(string name)
        {
            var containers = await GetContainersAsync();
            return containers.Where(x => x.Name == name).Any();
        }

        public async Task<List<Container>> GetContainersAsync()
        {
            using var client = new DockerClientConfiguration(new Uri(GetDockerApiUri())).CreateClient();
            var contrainers = await client.Containers.ListContainersAsync(new Docker.DotNet.Models.ContainersListParameters
            {
                All = true
            });

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

        public async Task StopAsync(string id)
        {
            using var client = new DockerClientConfiguration(new Uri(GetDockerApiUri())).CreateClient();
            await client.Containers.StopContainerAsync(id, new ContainerStopParameters
            {
            });
        }
        public async Task StartAsync(string id)
        {
            using var client = new DockerClientConfiguration(new Uri(GetDockerApiUri())).CreateClient();
            await client.Containers.StartContainerAsync(id, new ContainerStartParameters
            {
            });
        }
    }
}
