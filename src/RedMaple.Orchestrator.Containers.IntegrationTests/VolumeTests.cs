using Docker.DotNet.Models;

using RedMaple.Orchestrator.Containers;
using RedMaple.Orchestrator.Contracts.Containers;

namespace RedMaple.Orchestration.Containers.IntegrationTests
{
    public class VolumeTests
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

        private VolumesCreateParameters CreateVolumeParameters(string name, ContainerBackend backend)
        {
            return new VolumesCreateParameters
            {
                Name = name,
                Driver = backend == ContainerBackend.WslContainers ? "guest" : "local"
            };
        }

        [Theory]
        [InlineData(ContainerBackend.Docker)]
        [InlineData(ContainerBackend.Podman)]
        [InlineData(ContainerBackend.WslContainers)]
        public async Task ListVolumes_IsSuccessful(ContainerBackend backend)
        {
            using var fixture = new LocalContainerBackendFixture(backend);
            var client = fixture.LocalContainersClient;

            var volumes = await client.ListVolumesAsync(default);

            Assert.NotNull(volumes);
            Assert.NotNull(volumes.Volumes);
        }

        [Theory]
        [InlineData(ContainerBackend.Docker)]
        [InlineData(ContainerBackend.Podman)]
        [InlineData(ContainerBackend.WslContainers)]
        public async Task CreateVolume_IsSuccessful(ContainerBackend backend)
        {
            using var fixture = new LocalContainerBackendFixture(backend);
            var client = fixture.LocalContainersClient;

            var volumeName = $"test-volume-{Guid.NewGuid().ToString()[..8]}";
            var volumeResponse = await client.CreateVolumeAsync(CreateVolumeParameters(volumeName, backend), default);

            Assert.NotNull(volumeResponse);
            Assert.Equal(volumeName, volumeResponse.Name);
            Assert.Equal(backend == ContainerBackend.WslContainers ? "guest" : "local", volumeResponse.Driver);

            try
            {
                // Verify volume exists in listing
                var volumes = await client.ListVolumesAsync(default);
                var created = volumes.Volumes?.FirstOrDefault(v => v.Name == volumeName);
                Assert.NotNull(created);
                Assert.Equal(volumeName, created.Name);
            }
            finally
            {
                // Cleanup
                try
                {
                    await client.DeleteVolumeAsync(volumeName, false, default);
                }
                catch { }
            }
        }

        [Theory]
        [InlineData(ContainerBackend.Docker)]
        [InlineData(ContainerBackend.Podman)]
        [InlineData(ContainerBackend.WslContainers)]
        public async Task CreateContainer_WithVolume_IsSuccessful(ContainerBackend backend)
        {
            using var fixture = new LocalContainerBackendFixture(backend);
            var client = fixture.LocalContainersClient;
            var image = await GetAlpineImageAsync(client);

            var volumeName = $"test-volume-{Guid.NewGuid().ToString()[..8]}";
            var containerName = $"test-container-{Guid.NewGuid().ToString()[..8]}";
            const string containerMountPath = "/mnt/test";

            // Create volume
            var volumeResponse = await client.CreateVolumeAsync(CreateVolumeParameters(volumeName, backend), default);

            Assert.NotNull(volumeResponse);

            var containerId = string.Empty;
            try
            {
                // Create container with volume mount
                var createResponse = await client.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = image,
                    Name = containerName,
                    Cmd = new List<string> { "sleep", "1000" },
                    Volumes = new Dictionary<string, EmptyStruct>
                    {
                        { containerMountPath, default }
                    },
                    HostConfig = new HostConfig
                    {
                        Binds = new List<string>
                        {
                            $"{volumeName}:{containerMountPath}"
                        }
                    }
                }, default);

                containerId = createResponse?.ID;
                Assert.True(!string.IsNullOrEmpty(containerId), "Container ID should not be null");

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
                    await client.DeleteVolumeAsync(volumeName, false, default);
                }
                catch { }
            }
        }

        [Theory]
        [InlineData(ContainerBackend.Docker)]
        [InlineData(ContainerBackend.Podman)]
        [InlineData(ContainerBackend.WslContainers)]
        public async Task DeleteVolume_IsSuccessful(ContainerBackend backend)
        {
            using var fixture = new LocalContainerBackendFixture(backend);
            var client = fixture.LocalContainersClient;

            var volumeName = $"test-volume-{Guid.NewGuid().ToString()[..8]}";

            // Create volume
            var volumeResponse = await client.CreateVolumeAsync(CreateVolumeParameters(volumeName, backend), default);

            Assert.NotNull(volumeResponse);

            // Delete volume
            await client.DeleteVolumeAsync(volumeName, false, default);

            // Verify volume is deleted
            var volumes = await client.ListVolumesAsync(default);
            var deleted = volumes.Volumes?.FirstOrDefault(v => v.Name == volumeName);
            Assert.Null(deleted);
        }

        [Theory]
        [InlineData(ContainerBackend.Docker)]
        [InlineData(ContainerBackend.Podman)]
        [InlineData(ContainerBackend.WslContainers)]
        public async Task CreateContainer_WithMultipleVolumes_IsSuccessful(ContainerBackend backend)
        {
            using var fixture = new LocalContainerBackendFixture(backend);
            var client = fixture.LocalContainersClient;
            var image = await GetAlpineImageAsync(client);

            var volume1Name = $"test-volume-1-{Guid.NewGuid().ToString()[..8]}";
            var volume2Name = $"test-volume-2-{Guid.NewGuid().ToString()[..8]}";
            var containerName = $"test-container-{Guid.NewGuid().ToString()[..8]}";

            // Create volumes
            await client.CreateVolumeAsync(CreateVolumeParameters(volume1Name, backend), default);
            await client.CreateVolumeAsync(CreateVolumeParameters(volume2Name, backend), default);

            var containerId = string.Empty;
            try
            {
                // Create container with multiple volume mounts
                var createResponse = await client.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = image,
                    Name = containerName,
                    Cmd = new List<string> { "sleep", "1000" },
                    Volumes = new Dictionary<string, EmptyStruct>
                    {
                        { "/mnt/vol1", default },
                        { "/mnt/vol2", default }
                    },
                    HostConfig = new HostConfig
                    {
                        Binds = new List<string>
                        {
                            $"{volume1Name}:/mnt/vol1",
                            $"{volume2Name}:/mnt/vol2"
                        }
                    }
                }, default);

                containerId = createResponse?.ID;
                Assert.True(!string.IsNullOrEmpty(containerId), "Container ID should not be null");

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
                    await client.DeleteVolumeAsync(volume1Name, false, default);
                }
                catch { }
                try
                {
                    await client.DeleteVolumeAsync(volume2Name, false, default);
                }
                catch { }
            }
        }
    }
}
