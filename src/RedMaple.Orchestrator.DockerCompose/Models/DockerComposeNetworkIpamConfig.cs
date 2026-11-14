using System;
using System.Collections.Generic;

namespace RedMaple.Orchestrator.DockerCompose.Models
{
    public class DockerComposeNetworkIpamConfig
    {
        public string? subnet { get; set; }
        public string? ip_range { get; set; }
        public string? gateway { get; set; }
        public Dictionary<string, string>? aux_addresses { get; set; }
    }
}
