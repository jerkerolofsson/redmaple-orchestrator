using Docker.DotNet.Models;

using RedMaple.Orchestrator.Containers;
using RedMaple.Orchestrator.Contracts.Containers;

namespace RedMaple.Orchestration.Containers.IntegrationTests
{
    public class CreateTests
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
        public async Task CreateContainer_IsSuccessful(ContainerBackend backend)
        {
            using var fixture = new LocalContainerBackendFixture(backend);
            var client = fixture.LocalContainersClient;
            var image = await GetAlpineImageAsync(client);

            var containerName = $"test-create-{Guid.NewGuid().ToString()[..8]}";
            var response = await client.CreateContainerAsync(new CreateContainerParameters
            {
                Image = image,
                Name = containerName,
                Cmd = new List<string> { "sleep", "1000" }
            }, default);

            try
            {
                Assert.NotNull(response);

                // WslContainers may not be available
                if (string.IsNullOrEmpty(response.ID))
                {
                    Assert.Fail("Container ID should not be null");
                }

                // Verify container exists in listing
                var containers = await client.GetContainersAsync(null, default);
                var created = containers.FirstOrDefault(c => c.Id == response.ID);
                Assert.NotNull(created);
                Assert.Equal(containerName, created.Name);
            }
            finally
            {
                // Cleanup
                if (response?.ID != null)
                {
                    try
                    {
                        await client.DeleteContainerAsync(response.ID, default);
                    }
                    catch { }
                }
            }
        }

        [Theory]
        [InlineData(ContainerBackend.Docker)]
        [InlineData(ContainerBackend.Podman)]
        [InlineData(ContainerBackend.WslContainers)]
        public async Task CreateAndDeleteContainer_ContainerIsRemovedFromListing(ContainerBackend backend)
        {
            using var fixture = new LocalContainerBackendFixture(backend);
            var client = fixture.LocalContainersClient;
            var image = await GetAlpineImageAsync(client);

            var containerName = $"test-delete-{Guid.NewGuid().ToString()[..8]}";
            var response = await client.CreateContainerAsync(new CreateContainerParameters
            {
                Image = image,
                Name = containerName,
                Cmd = new List<string> { "sleep", "1000" }
            }, default);

            var containerId = response?.ID;
            Assert.True(!string.IsNullOrEmpty(containerId), "Container ID should not be null");

            try
            {
                // Verify container exists
                var containersBefore = await client.GetContainersAsync(null, default);
                var found = containersBefore.FirstOrDefault(c => c.Id == containerId);
                Assert.NotNull(found);

                // Delete container
                await client.DeleteContainerAsync(containerId, default);

                // Verify container is removed
                var containersAfter = await client.GetContainersAsync(null, default);
                Assert.DoesNotContain(containersAfter, c => c.Id == containerId);
            }
            finally
            {
                try
                {
                    if (containerId != null)
                    {
                        await client.DeleteContainerAsync(containerId, default);
                    }
                }
                catch { }
            }
        }

        [Theory]
        [InlineData(ContainerBackend.Docker)]
        [InlineData(ContainerBackend.Podman)]
        [InlineData(ContainerBackend.WslContainers)]
        public async Task CreateContainer_WithVolumeMount_IsSuccessful(ContainerBackend backend)
        {
            using var fixture = new LocalContainerBackendFixture(backend);
            using var tempDir = new TempDirectory();
            var client = fixture.LocalContainersClient;
            var image = await GetAlpineImageAsync(client);

            var containerName = $"test-volume-{Guid.NewGuid().ToString()[..8]}";
            var volumePath = "/mnt/volume";

            var response = await client.CreateContainerAsync(new CreateContainerParameters
            {
                Image = image,
                Name = containerName,
                Cmd = new List<string> { "sleep", "1000" },
                Volumes = new Dictionary<string, EmptyStruct>
                {
                    { volumePath, default }
                },
                HostConfig = new HostConfig
                {
                    Binds = new List<string> { $"{tempDir.Path}:{volumePath}" }
                }
            }, default);

            var containerId = response?.ID;

            // WslContainers may not be available
            if (string.IsNullOrEmpty(containerId))
            {
                Assert.True(false, "Container ID should not be null");
            }

            try
            {
                // Verify container exists
                var container = await client.GetContainerByIdAsync(containerId, default);
                Assert.NotNull(container);
            }
            finally
            {
                try
                {
                    if (containerId != null)
                    {
                        await client.DeleteContainerAsync(containerId, default);
                    }
                }
                catch { }
            }
        }

        [Theory]
        [InlineData(ContainerBackend.Docker)]
        [InlineData(ContainerBackend.Podman)]
        [InlineData(ContainerBackend.WslContainers)]
        public async Task CreateContainer_WithVolumeMount_ExecAndVerifyFile(ContainerBackend backend)
        {
            using var fixture = new LocalContainerBackendFixture(backend);
            using var tempDir = new TempDirectory();
            var client = fixture.LocalContainersClient;
            var image = await GetAlpineImageAsync(client);

            var containerName = $"test-exec-{Guid.NewGuid().ToString()[..8]}";
            var volumePath = "/volume";
            var testFile = "test.txt";

            var response = await client.CreateContainerAsync(new CreateContainerParameters
            {
                Image = image,
                Name = containerName,
                Cmd = new List<string> { "sleep", "1000" },
                Volumes = new Dictionary<string, EmptyStruct>
                {
                    { volumePath, default }
                },
                HostConfig = new HostConfig
                {
                    Binds = new List<string> { $"{tempDir.Path}:{volumePath}" }
                }
            }, default);

            var containerId = response?.ID;
            Assert.True(!string.IsNullOrEmpty(containerId), "Container ID should not be null");

            try
            {
                // Start the container
                await client.StartAsync(containerId, default);

                // Give container time to start
                await Task.Delay(500);

                // Execute command to write a file to the bound volume
                await client.ExecAsync(containerId, new List<string> { "sh", "-c", $"echo ',,,' > {volumePath}/{testFile}" }, default);

                // Verify file exists on the host
                var filePath = System.IO.Path.Combine(tempDir.Path, testFile);
                Assert.True(File.Exists(filePath), $"File not found at {filePath}");

                var content = File.ReadAllText(filePath).Trim();
                Assert.Equal(",,,", content);

            }
            finally
            {
                try
                {
                    if (containerId != null)
                    {
                        await client.StopAsync(containerId, default);
                    }
                }
                catch { }
                try
                {
                    if (containerId != null)
                    {
                        await client.DeleteContainerAsync(containerId, default);
                    }
                }
                catch { }
            }
        }
    }
}
