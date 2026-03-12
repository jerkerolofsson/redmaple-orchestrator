
using RedMaple.Orchestrator.DockerCompose.Models;

namespace RedMaple.Orchestrator.DockerCompose
{
    public interface IDockerCompose
    {
        /// <summary>
        /// Takes down the container stack specified in the plan
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="plan"></param>
        /// <param name="environmentFile"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DownAsync(IProgress<string> progress, DockerComposePlan plan, string? environmentFile, CancellationToken cancellationToken);

        /// <summary>
        /// Brings up the container stack specified in the plan
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="plan"></param>
        /// <param name="environmentFile"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task UpAsync(IProgress<string> progress, DockerComposePlan plan, string? environmentFile, CancellationToken cancellationToken);
    }
}