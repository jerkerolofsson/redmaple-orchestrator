using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.DockerCompose.Models
{
    public class DockerComposeNetwork
    {
        public string? driver { get; set; }
        public string? scope { get; set; }

        [YamlMember(Alias = "internal")]
        public bool? Internal { get; set; }
    }
}
