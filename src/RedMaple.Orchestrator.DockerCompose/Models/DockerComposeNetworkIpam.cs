using System;
using System.Collections.Generic;

namespace RedMaple.Orchestrator.DockerCompose.Models
{
    public class DockerComposeNetworkIpam
    {
        public string? driver { get; set; }
        public List<DockerComposeNetworkIpamConfig>? config { get; set; }
        public Dictionary<string, string>? options { get; set; }
    }
}
