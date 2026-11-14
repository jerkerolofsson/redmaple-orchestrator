using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.DockerCompose.Models
{
    /// <summary>
    /// Represents a Docker Compose volume configuration.
    /// Based on Docker Compose specification: https://github.com/compose-spec/compose-spec/blob/main/07-volumes.md
    /// </summary>
    public class DockerComposeVolume
    {
        /// <summary>
        /// Specifies which volume driver should be used. Default and available values are platform specific.
        /// </summary>
        public string? driver { get; set; }

        /// <summary>
        /// Add metadata to volumes using labels. Can be either an array or a dictionary.
        /// Recommended to use reverse-DNS notation to prevent conflicts.
        /// </summary>
        public List<string>? labels { get; set; }

        /// <summary>
        /// Specifies a list of options as key-value pairs to pass to the driver for this volume.
        /// The options are driver-dependent.
        /// </summary>
        public DockerComposeVolumeDriverOpts? driver_opts { get; set; }

        /// <summary>
        /// If set to true, external specifies that this volume already exists on the platform 
        /// and its lifecycle is managed outside of that of the application.
        /// Compose doesn't then create the volume, and returns an error if the volume doesn't exist.
        /// </summary>
        public bool? external { get; set; }

        /// <summary>
        /// Sets a custom name for a volume. The name is used as is and is not scoped with the stack name.
        /// This makes it possible to make this lookup name a parameter of the Compose file.
        /// </summary>
        public string? name { get; set; }
    }
}
