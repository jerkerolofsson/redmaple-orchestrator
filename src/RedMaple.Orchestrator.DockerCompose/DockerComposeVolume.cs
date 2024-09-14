using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.DockerCompose
{
    public class DockerComposeVolume
    {
        public string? driver { get; set; }
        public List<string>? labels { get; set; }
        public DockerComposeVolumeDriverOpts? driver_opts { get; set; }
        public bool? external { get; set; }
    }
}
