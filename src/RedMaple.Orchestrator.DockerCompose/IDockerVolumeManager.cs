
using Docker.DotNet.Models;
using RedMaple.Orchestrator.Contracts.Deployments;

namespace RedMaple.Orchestrator.DockerCompose
{
    public interface IDockerVolumeManager
    {
        Task CreateVolumesAsync(List<VolumeBind> volumeBinds, IProgress<string> progress, CancellationToken cancellationToken);
        Task<CreateVolumeResult> TryCreateVolumeAsync(VolumesCreateParameters volumesCreateParameters, CancellationToken cancellationToken);
    }
}