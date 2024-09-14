using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.DockerCompose.Models
{
    public class DockerComposeServiceVolumeBind
    {
        public string? Propagation { get; set; }
        public string? CreateHostPath { get; set; }
        public string? SeLinux { get; set; }
    }
}
