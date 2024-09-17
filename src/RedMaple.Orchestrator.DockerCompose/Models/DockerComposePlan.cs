using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.DockerCompose.Models
{
    public class DockerComposePlan
    {
        public string? name { get; set; }
        public string? version { get; set; }

        public Dictionary<string, DockerComposeVolume> volumes { get; set; } = new();
        public Dictionary<string, DockerComposeService> services { get; set; } = new();
        public Dictionary<string, DockerComposeNetwork> networks { get; set; } = new();
        //public Dictionary<string, DockerComposeNetwork?> configs { get; set; } = new Dictionary<string, DockerComposeNetwork?>();
        //public Dictionary<string, DockerComposeNetwork?> secrets { get; set; } = new Dictionary<string, DockerComposeNetwork?>();

        /// <summary>
        /// Global labels
        /// </summary>
        public Dictionary<string, string> Labels { get; set; } = new();
        public IReadOnlyList<string>? RequiredEnvironmentVariables { get; internal set; }

        /// <summary>
        /// Keep the original yaml, as we will transform it with environment variables
        /// </summary>
        public string Yaml { get; set; } = "";

        public string ProjectName
        {
            get
            {
                if (!Labels.TryGetValue(DockerComposeConstants.LABEL_PROJECT, out var planProject))
                {
                    throw new Exception("Plan label is missing: " + DockerComposeConstants.LABEL_PROJECT);
                }
                return planProject;
            }
        }
    }
}
