using Docker.DotNet.Models;

using Microsoft.Extensions.Logging;

using RedMaple.Orchestrator.Containers;
using RedMaple.Orchestrator.Contracts.Containers;
using RedMaple.Orchestrator.Contracts.Deployments;

namespace RedMaple.Orchestrator.DockerCompose
{
    public class DockerVolumeManager : IDockerVolumeManager
    {
        private readonly ILocalContainersClient _docker;
        private readonly ILogger<DockerVolumeManager> _logger;

        public DockerVolumeManager(ILocalContainersClientFactory localContainersClientFactory, ILogger<DockerVolumeManager> logger)
        {
            _docker = localContainersClientFactory.Create();
            _logger = logger;
        }

        public string GetDefaultDriver()
        {
            return _docker.GetDefaultVolumeDriver();
        }

        public async Task CreateVolumesAsync(List<VolumeBind> volumeBinds, IProgress<string> progress, CancellationToken cancellationToken)
        {
            foreach (var volumeBind in volumeBinds)
            {
                if (volumeBind.Volume is not null)
                {
                    List<string> driverOpts = new();

                    if (!string.IsNullOrWhiteSpace(volumeBind.Volume.Username))
                    {
                        driverOpts.Add($"username={volumeBind.Volume.Username}");
                    }
                    if (!string.IsNullOrWhiteSpace(volumeBind.Volume.Password))
                    {
                        driverOpts.Add($"password={volumeBind.Volume.Password}");
                    }
                    if (!string.IsNullOrWhiteSpace(volumeBind.Volume.Server))
                    {
                        driverOpts.Add($"addr={volumeBind.Volume.Server}");
                    }
                    if (!string.IsNullOrWhiteSpace(volumeBind.Volume.FileMode))
                    {
                        driverOpts.Add($"file_mode={volumeBind.Volume.FileMode}");
                    }
                    if (!string.IsNullOrWhiteSpace(volumeBind.Volume.DirMode))
                    {
                        driverOpts.Add($"dir_mode={volumeBind.Volume.DirMode}");
                    }
                    if (!string.IsNullOrWhiteSpace(volumeBind.Volume.AccessMode))
                    {
                        driverOpts.Add($"{volumeBind.Volume.AccessMode}");
                    }

                    var opts = new Dictionary<string, string>()
                    {
                        ["type"] = volumeBind.Volume.Type ?? "cifs",
                        ["device"] = volumeBind.Volume.Location ?? throw new ArgumentException("Location not specified"),
                        ["o"] = string.Join(",", driverOpts),
                    };
                    //username=myusername,password=mypasswd,uid=500,gid=499,rw

                    var parameters = new VolumesCreateParameters
                    {
                        Name = volumeBind.Name,
                        Driver = volumeBind?.Volume?.Driver ?? _docker.GetDefaultVolumeDriver(),
                        DriverOpts = opts,
                    };

                    var result = await TryCreateVolumeAsync(parameters, cancellationToken);
                    if(result == CreateVolumeResult.VolumeAlreadyExists)
                    {
                        // Todo: Diff to see if we need to recreate the volume or not
                        //await RemoveVolumeAsync(volumeBind.Name, cancellationToken);
                        //await TryCreateVolumeAsync(parameters, cancellationToken);
                    }
                }
            }
        }

        public async Task<bool> RemoveVolumeAsync(string name, CancellationToken cancellationToken)
        {
            var existingVolumes = await _docker.ListVolumesAsync(cancellationToken);
            var existingVolume = existingVolumes.Volumes.Where(x => x.Name == name).FirstOrDefault();
            if (existingVolume is not null)
            {
                await _docker.RemoveVolumeAsync(name, false, cancellationToken);
            }

            return false;
        }
        public async Task<CreateVolumeResult> TryCreateVolumeAsync(VolumesCreateParameters volumesCreateParameters, CancellationToken cancellationToken)
        {
            var name = volumesCreateParameters.Name;

            var existingVolumes = await _docker.ListVolumesAsync(cancellationToken);
            var existingVolume = existingVolumes.Volumes.Where(x => x.Name == name).FirstOrDefault();
            if(existingVolume is not null)
            {
                return CreateVolumeResult.VolumeAlreadyExists;
            }

            if (existingVolume is null)
            {
                var response = await _docker.CreateVolumeAsync(volumesCreateParameters, cancellationToken);
                return CreateVolumeResult.Success;
            }

            return CreateVolumeResult.Error;
        }
    }
}
