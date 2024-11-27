using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RedMaple.Orchestrator.DockerCompose.Converters;

namespace RedMaple.Orchestrator.DockerCompose.Models
{
    /// <summary>
    /// https://docs.docker.com/reference/compose-file/services/
    /// </summary>
    public class DockerComposeService
    {
        public string? image { get; set; }
        public string? container_name { get; set; }
        public string? hostname { get; set; }
        public string? domainname { get; set; }
        public string? restart { get; set; }

        public List<DockerComposePortMapping>? ports { get; set; }
        //public List<DockerComposeDeviceReservation>? devices { get; set; }
        public DockerComposeDeploy? deploy { get; set; }
        public List<string>? dns { get; set; }
        public List<string>? dns_opt { get; set; }
        public List<string>? dns_search { get; set; }
        public List<string>? depends_on { get; set; }

        public Command? command { get; set; }
        public List<string>? cap_add { get; set; }
        public List<string>? networks { get; set; }

        public List<DockerComposeServiceVolume> volumes { get; set; } = new();

        public EnvironmentVariables? environment { get; set; }
    }
}
