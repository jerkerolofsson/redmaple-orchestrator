using Docker.DotNet.Models;

using RedMaple.Orchestrator.Containers;
using RedMaple.Orchestrator.Contracts.Containers;

namespace RedMaple.Orchestration.Containers.IntegrationTests
{
    public class NetworkTests
    {
        private async Task<string> GetAlpineImageAsync(ILocalContainersClient client)
        {
            const string image = "alpine:latest";
            try
            {
                if (!await client.HasImageAsync(image, default))
                {
                    await client.PullImageAsync(image, IProgress<JSONMessage>.NullProgress, null, default);
                }
            }
            catch
            {
                // Fallback: try the image anyway, might already be there
            }
            return image;
        }

        [Theory]
        [InlineData(ContainerBackend.Docker)]
        [InlineData(ContainerBackend.Podman)]
        [InlineData(ContainerBackend.WslContainers)]
        public async Task ListNetworks_IsSuccessful(ContainerBackend backend)
        {
            using var fixture = new LocalContainerBackendFixture(backend);
            var client = fixture.LocalContainersClient;

            var networks = await client.ListNetworksAsync(new NetworksListParameters(), default);

            Assert.NotNull(networks);
            Assert.True(networks.Count > 0, "Should have at least the default networks");
        }

        [Theory]
        [InlineData(ContainerBackend.Docker)]
        [InlineData(ContainerBackend.Podman)]
        [InlineData(ContainerBackend.WslContainers)]
        public async Task CreateNetwork_IsSuccessful(ContainerBackend backend)
        {
            using var fixture = new LocalContainerBackendFixture(backend);
            var client = fixture.LocalContainersClient;

            var networkName = $"test-network-{Guid.NewGuid().ToString()[..8]}";
            var networkResponse = await client.CreateNetworkAsync(new NetworksCreateParameters
            {
                Name = networkName,
                Driver = "bridge"
            }, default);

            Assert.NotNull(networkResponse);
            Assert.NotNull(networkResponse.ID);

            var networkId = networkResponse.ID;
            try
            {
                // Verify network exists in listing
                var networks = await client.ListNetworksAsync(new NetworksListParameters(), default);
                var created = networks.FirstOrDefault(n => n.Name == networkName);
                Assert.NotNull(created);
                Assert.Equal(networkName, created.Name);
            }
            finally
            {
                // Cleanup
                try
                {
                    await client.DeleteNetworkAsync(networkId, new ContainerRemoveParameters(), default);
                }
                catch { }
            }
        }

        [Theory]
        [InlineData(ContainerBackend.Docker)]
        [InlineData(ContainerBackend.Podman)]
        [InlineData(ContainerBackend.WslContainers)]
        public async Task CreateContainer_WithNetwork_IsSuccessful(ContainerBackend backend)
        {
            using var fixture = new LocalContainerBackendFixture(backend);
            var client = fixture.LocalContainersClient;
            var image = await GetAlpineImageAsync(client);

            var networkName = $"test-network-{Guid.NewGuid().ToString()[..8]}";
            var containerName = $"test-container-{Guid.NewGuid().ToString()[..8]}";

            // Create network
            var networkResponse = await client.CreateNetworkAsync(new NetworksCreateParameters
            {
                Name = networkName,
                Driver = "bridge"
            }, default);

            Assert.NotNull(networkResponse);
            var networkId = networkResponse.ID;

            var containerId = string.Empty;
            try
            {
                // Create container
                var createResponse = await client.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = image,
                    Name = containerName,
                    Cmd = new List<string> { "sleep", "1000" },
                    HostConfig = new HostConfig
                    {
                        NetworkMode = networkName
                    }
                }, default);

                containerId = createResponse?.ID;
                Assert.True(!string.IsNullOrEmpty(containerId), "Container ID should not be null");

                // Connect container to network
                await client.ConnectNetworkAsync(networkId, new NetworkConnectParameters
                {
                    Container = containerId
                }, default);

                // Start the container
                await client.StartAsync(containerId, default);

                // Verify container exists in listing
                var containers = await client.GetContainersAsync(null, default);
                var container = containers.FirstOrDefault(c => c.Id == containerId);
                Assert.NotNull(container);
                Assert.Equal(containerName, container.Name);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(containerId))
                {
                    try
                    {
                        await client.StopAsync(containerId, default);
                    }
                    catch { }
                    try
                    {
                        await client.DeleteContainerAsync(containerId, default);
                    }
                    catch { }
                }
                try
                {
                    await client.DeleteNetworkAsync(networkId, new ContainerRemoveParameters(), default);
                }
                catch { }
            }
        }

        [Theory]
        [InlineData(ContainerBackend.Docker)]
        [InlineData(ContainerBackend.Podman)]
        [InlineData(ContainerBackend.WslContainers)]
        public async Task DeleteNetwork_IsSuccessful(ContainerBackend backend)
        {
            using var fixture = new LocalContainerBackendFixture(backend);
            var client = fixture.LocalContainersClient;

            var networkName = $"test-network-{Guid.NewGuid().ToString()[..8]}";

            // Create network
            var networkResponse = await client.CreateNetworkAsync(new NetworksCreateParameters
            {
                Name = networkName,
                Driver = "bridge"
            }, default);

            Assert.NotNull(networkResponse);
            var networkId = networkResponse.ID;

            // Delete network
            await client.DeleteNetworkAsync(networkId, new ContainerRemoveParameters(), default);

            // Verify network is deleted
            var networks = await client.ListNetworksAsync(new NetworksListParameters(), default);
            var deleted = networks.FirstOrDefault(n => n.Name == networkName);
            Assert.Null(deleted);
        }

        [Theory]
        [InlineData(ContainerBackend.Docker)]
        [InlineData(ContainerBackend.Podman)]
        [InlineData(ContainerBackend.WslContainers)]
        public async Task CreateMultipleContainers_OnSameNetwork_IsSuccessful(ContainerBackend backend)
        {
            using var fixture = new LocalContainerBackendFixture(backend);
            var client = fixture.LocalContainersClient;
            var image = await GetAlpineImageAsync(client);

            var networkName = $"test-network-{Guid.NewGuid().ToString()[..8]}";
            var container1Name = $"test-container-1-{Guid.NewGuid().ToString()[..8]}";
            var container2Name = $"test-container-2-{Guid.NewGuid().ToString()[..8]}";

            // Create network
            var networkResponse = await client.CreateNetworkAsync(new NetworksCreateParameters
            {
                Name = networkName,
                Driver = "bridge"
            }, default);

            Assert.NotNull(networkResponse);
            var networkId = networkResponse.ID;

            var container1Id = string.Empty;
            var container2Id = string.Empty;
            try
            {
                // Create first container
                var create1Response = await client.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = image,
                    Name = container1Name,
                    Cmd = new List<string> { "sleep", "1000" },
                    HostConfig = new HostConfig
                    {
                        NetworkMode = networkName
                    }
                }, default);

                container1Id = create1Response?.ID;
                Assert.True(!string.IsNullOrEmpty(container1Id), "Container 1 ID should not be null");

                // Create second container
                var create2Response = await client.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = image,
                    Name = container2Name,
                    Cmd = new List<string> { "sleep", "1000" },
                    HostConfig = new HostConfig
                    {
                        NetworkMode = networkName
                    }
                }, default);

                container2Id = create2Response?.ID;
                Assert.True(!string.IsNullOrEmpty(container2Id), "Container 2 ID should not be null");

                // Connect both containers to the network
                await client.ConnectNetworkAsync(networkId, new NetworkConnectParameters
                {
                    Container = container1Id
                }, default);

                await client.ConnectNetworkAsync(networkId, new NetworkConnectParameters
                {
                    Container = container2Id
                }, default);

                // Start both containers
                await client.StartAsync(container1Id, default);
                await client.StartAsync(container2Id, default);

                // Verify both containers exist in listing
                var containers = await client.GetContainersAsync(null, default);
                var container1 = containers.FirstOrDefault(c => c.Id == container1Id);
                var container2 = containers.FirstOrDefault(c => c.Id == container2Id);

                Assert.NotNull(container1);
                Assert.NotNull(container2);
            }
            finally
            {
                // Cleanup
                if (!string.IsNullOrEmpty(container1Id))
                {
                    try
                    {
                        await client.StopAsync(container1Id, default);
                    }
                    catch { }
                    try
                    {
                        await client.DeleteContainerAsync(container1Id, default);
                    }
                    catch { }
                }
                if (!string.IsNullOrEmpty(container2Id))
                {
                    try
                    {
                        await client.StopAsync(container2Id, default);
                    }
                    catch { }
                    try
                    {
                        await client.DeleteContainerAsync(container2Id, default);
                    }
                    catch { }
                }
                try
                {
                    await client.DeleteNetworkAsync(networkId, new ContainerRemoveParameters(), default);
                }
                catch { }
            }
        }

        [Theory]
        [InlineData(ContainerBackend.Docker)]
        [InlineData(ContainerBackend.Podman)]
        [InlineData(ContainerBackend.WslContainers)]
        public async Task CreateNetwork_WithCustomSubnet_IsSuccessful(ContainerBackend backend)
        {
            using var fixture = new LocalContainerBackendFixture(backend);
            var client = fixture.LocalContainersClient;

            var networkName = $"test-network-{Guid.NewGuid().ToString()[..8]}";
            var subnet = "172.20.0.0/16";
            var gateway = "172.20.0.1";

            var networkResponse = await client.CreateNetworkAsync(new NetworksCreateParameters
            {
                Name = networkName,
                Driver = "bridge",
                IPAM = new IPAM
                {
                    Config = new List<IPAMConfig>
                    {
                        new IPAMConfig
                        {
                            Subnet = subnet,
                            Gateway = gateway
                        }
                    }
                }
            }, default);

            Assert.NotNull(networkResponse);
            var networkId = networkResponse.ID;

            try
            {
                // Verify network exists with custom subnet
                var networks = await client.ListNetworksAsync(new NetworksListParameters(), default);
                var created = networks.FirstOrDefault(n => n.Name == networkName);
                Assert.NotNull(created);
                Assert.Equal(networkName, created.Name);

                // Verify IPAM configuration
                Assert.NotNull(created.IPAM);
                Assert.NotNull(created.IPAM.Config);
                Assert.True(created.IPAM.Config.Count > 0, "Network should have IPAM config");
            }
            finally
            {
                // Cleanup
                try
                {
                    await client.DeleteNetworkAsync(networkId, new ContainerRemoveParameters(), default);
                }
                catch { }
            }
        }
    }
}
