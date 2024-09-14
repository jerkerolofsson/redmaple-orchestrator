using RedMaple.Orchestrator.DockerCompose;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.DockerCompose
{
    public class DockerComposePlan
    {
        public string? name { get; set; }
        public string? version { get; set; }

        public Dictionary<string, DockerComposeVolume?> volumes { get; set; } = new Dictionary<string, DockerComposeVolume?>();
        public Dictionary<string, DockerComposeService?> services { get; set; } = new Dictionary<string, DockerComposeService?>();
        public Dictionary<string, DockerComposeNetwork?> networks { get; set; } = new Dictionary<string, DockerComposeNetwork?>();
        //public Dictionary<string, DockerComposeNetwork?> configs { get; set; } = new Dictionary<string, DockerComposeNetwork?>();
        //public Dictionary<string, DockerComposeNetwork?> secrets { get; set; } = new Dictionary<string, DockerComposeNetwork?>();


    }
}
