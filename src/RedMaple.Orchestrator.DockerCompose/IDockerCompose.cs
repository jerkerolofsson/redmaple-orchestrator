
using RedMaple.Orchestrator.DockerCompose.Models;

namespace RedMaple.Orchestrator.DockerCompose
{
    public interface IDockerCompose
    {
        Task DownAsync(IProgress<string> progress, DockerComposePlan plan, string? environmentFile, CancellationToken cancellationToken);
        Task UpAsync(IProgress<string> progress, DockerComposePlan plan, string? environmentFile, CancellationToken cancellationToken);
    }
}