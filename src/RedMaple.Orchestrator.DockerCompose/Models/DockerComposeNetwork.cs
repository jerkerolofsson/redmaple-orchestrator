using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.DockerCompose.Models
{
    /// <summary>
    /// Represents a Docker Compose network configuration.
    /// Based on Docker Compose specification: https://github.com/compose-spec/compose-spec/blob/main/06-networks.md
    /// </summary>
    public class DockerComposeNetwork
    {
        /// <summary>
        /// Specifies which driver should be used for this network.
        /// </summary>
        public string? driver { get; set; }

        /// <summary>
        /// Specifies the scope of the network.
        /// </summary>
        public string? scope { get; set; }

        /// <summary>
        /// By default, Compose provides external connectivity to networks. 
        /// Internal, when set to true, allows you to create an externally isolated network.
        /// </summary>
        [YamlMember(Alias = "internal")]
        public bool? Internal { get; set; }

        /// <summary>
        /// Specifies a list of options as key-value pairs to pass to the driver.
        /// These options are driver-dependent.
        /// </summary>
        public DockerComposeNetworkDriverOpts? driver_opts { get; set; }

        /// <summary>
        /// If set to true, external specifies that this network's lifecycle is maintained outside of that of the application.
        /// Compose doesn't attempt to create these networks, and returns an error if one doesn't exist.
        /// </summary>
        public bool? external { get; set; }

        /// <summary>
        /// Specifies a custom IPAM configuration.
        /// </summary>
        public DockerComposeNetworkIpam? ipam { get; set; }

        /// <summary>
        /// Add metadata to containers using labels. You can use either an array or a dictionary.
        /// </summary>
        public Dictionary<string, string>? labels { get; set; }

        /// <summary>
        /// Sets a custom name for the network. The name is used as is and is not scoped with the project name.
        /// </summary>
        public string? name { get; set; }

        /// <summary>
        /// If attachable is set to true, then standalone containers should be able to attach to this network,
        /// in addition to services.
        /// </summary>
        public bool? attachable { get; set; }

        /// <summary>
        /// Can be used to disable IPv4 address assignment (Compose v2.33.1+).
        /// </summary>
        public bool? enable_ipv4 { get; set; }

        /// <summary>
        /// Enables IPv6 address assignment.
        /// </summary>
        public bool? enable_ipv6 { get; set; }
    }
}
