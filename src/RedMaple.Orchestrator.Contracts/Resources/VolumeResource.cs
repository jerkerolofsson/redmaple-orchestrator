using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Resources
{
    public class VolumeResource
    {
        public required string Name { get; set; }
        public string Driver { get; set; } = "local";
        public required string Slug { get; set; }
        public string? Location { get; set; }

        public string? Server { get; set; }

        public string? FileMode { get; set; }
        public string? DirMode { get; set; }

        public string AccessMode { get; set; } = "rw";
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? Type { get; set; }
    }
}
