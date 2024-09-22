using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.Contracts.Containers;
using RedMaple.Orchestrator.Contracts.Deployments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RedMaple.Orchestrator.DockerCompose
{
    public class DockerVolumeManager : IDockerVolumeManager
    {
        private readonly ILocalContainersClient _docker;
        private readonly ILogger<DockerVolumeManager> _logger;

        public DockerVolumeManager(ILocalContainersClient docker, ILogger<DockerVolumeManager> logger)
        {
            _docker = docker;
            _logger = logger;
        }

        public async Task CreateVolumesAsync(List<VolumeBind> volumeBinds, IProgress<string> progress, CancellationToken cancellationToken)
        {
            foreach (var volumeBind in volumeBinds)
            {
                if (volumeBind.Volume is not null)
                {
                    List<string> driverOpts = new();

                    if (volumeBind.Volume.Username != null)
                    {
                        driverOpts.Add($"username={volumeBind.Volume.Username}");
                    }
                    if (volumeBind.Volume.Password != null)
                    {
                        driverOpts.Add($"password={volumeBind.Volume.Password}");
                    }
                    if (volumeBind.Volume.Server != null)
                    {
                        driverOpts.Add($"addr={volumeBind.Volume.Server}");
                    }
                    if (volumeBind.Volume.FileMode != null)
                    {
                        driverOpts.Add($"file_mode={volumeBind.Volume.FileMode}");
                    }
                    if (volumeBind.Volume.DirMode != null)
                    {
                        driverOpts.Add($"dir_mode={volumeBind.Volume.DirMode}");
                    }
                    if (volumeBind.Volume.AccessMode != null)
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
                        Driver = "local",
                        DriverOpts = opts,
                    };

                    await TryCreateVolumeAsync(parameters, cancellationToken);
                }
            }
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
