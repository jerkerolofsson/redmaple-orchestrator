using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.DockerCompose.Models
{
    public class DockerComposeServiceVolume
    {
        /// <summary>
        /// Either volume, bind, tmpfs, npipe, or cluster
        /// </summary>
        public string? Type { get; internal set; }
        public string? Source { get; internal set; }
        public string? Target { get; internal set; }
        public string? ReadOnly { get; internal set; }
        public string? Consistency { get; internal set; }
        public string? AccessMode { get; internal set; }

        public string ToShortFormat(bool includeAccessMode = true)
        {
            var sb = new StringBuilder();
            sb.Append(Source);
            sb.Append(":");
            sb.Append(Target);
            if(AccessMode != null && includeAccessMode)
            {
                sb.Append(":");
                sb.Append(AccessMode);
            }
            return sb.ToString();
        }
    }
}
