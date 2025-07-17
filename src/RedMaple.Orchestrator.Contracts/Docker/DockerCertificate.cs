using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Docker
{
    /// <summary>
    /// A docker certificate
    /// </summary>
    public class DockerCertificate
    {
        /// <summary>
        /// Matches /etc/docker/certs.d/[RegistryDomain]
        /// </summary>
        public required string RegistryDomain { get; set; }

        /// <summary>
        /// PEM content of certificate
        /// </summary>
        public required string CaCrt { get; set; }
    }
}
