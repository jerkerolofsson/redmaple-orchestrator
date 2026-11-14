using Docker.DotNet.Models;

using RedMaple.Orchestrator.Containers;

namespace RedMaple.Orchestration.Containers.IntegrationTests
{
    public class ImageTests
    {
        [Theory]
        [InlineData(ContainerBackend.Docker)]
        [InlineData(ContainerBackend.Podman)]
        [InlineData(ContainerBackend.WslContainers)]
        public async Task PullImage_IsSuccessful(ContainerBackend backend)
        {
            using var fixture = new LocalContainerBackendFixture(backend);
            var client = fixture.LocalContainersClient;

            await client.PullImageAsync("registry:2", IProgress<JSONMessage>.NullProgress, null, default);
        }

        [Theory]
        [InlineData(ContainerBackend.Docker)]
        [InlineData(ContainerBackend.Podman)]
        [InlineData(ContainerBackend.WslContainers)]
        public async Task ListImages_IsSuccessful(ContainerBackend backend)
        {
            using var fixture = new LocalContainerBackendFixture(backend);
            var client = fixture.LocalContainersClient;
            await client.PullImageAsync("registry:2", IProgress<JSONMessage>.NullProgress, null, default);

            var response = await client.ListImagesAsync(default);

            var image = response.FirstOrDefault(i => i.RepoTags != null && i.RepoTags.Any(t => t.EndsWith("registry:2")));
            Assert.NotNull(image);
        }
    }
}
