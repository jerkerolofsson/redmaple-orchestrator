using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Docker.DotNet.Models;

using Microsoft.Extensions.Logging;

using RedMaple.Orchestrator.Containers;
using RedMaple.Orchestrator.Contracts.Containers;

namespace RedMaple.Orchestrator.DockerCompose
{
    public partial class DockerCompose : IDockerCompose
    {
        private readonly ILocalContainersClient _docker;
        private readonly IDockerVolumeManager _volumes;
        private readonly ILogger<DockerCompose> _logger;

        public DockerCompose(ILocalContainersClientFactory localContainersClientFactory, ILogger<DockerCompose> logger, IDockerVolumeManager volumes)
        {
            _docker = localContainersClientFactory.Create();
            _logger = logger;
            _volumes = volumes;
        }

    }
}
