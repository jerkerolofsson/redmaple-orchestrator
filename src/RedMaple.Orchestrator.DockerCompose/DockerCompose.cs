using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.Contracts.Containers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.DockerCompose
{
    public partial class DockerCompose : IDockerCompose
    {
        private readonly ILocalContainersClient _docker;
        private readonly ILogger<DockerCompose> _logger;

        public DockerCompose(ILocalContainersClient docker, ILogger<DockerCompose> logger)
        {
            _docker = docker;
            _logger = logger;
        }

    }
}
