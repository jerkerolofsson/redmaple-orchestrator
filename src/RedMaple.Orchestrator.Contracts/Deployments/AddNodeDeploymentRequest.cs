using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Deployments
{
    public class AddNodeDeploymentRequest
    {
        /// <summary>
        /// Deployment slug
        /// </summary>
        public required string Slug { get; set; }

        /// <summary>
        /// HTTPS certificate (pfx)
        /// </summary>
        public required byte[] HttpsCertificatePfx { get; set; }

        /// <summary>
        /// HTTPS certificate (pem)
        /// </summary>
        public required byte[] HttpsCertificatePemCert { get; set; }

        /// <summary>
        /// HTTPS certificate key (pem)
        /// </summary>
        public required byte[] HttpsCertificatePemKey { get; set; }

        /// <summary>
        /// Environment variables (set by the user when creating the deployment), and some automatic
        /// </summary>
        public required Dictionary<string, string> EnvironmentVariables { get; set; }

        public List<VolumeBind> VolumeBinds { get; set; } = new List<VolumeBind>();

        public required DeploymentKind Kind { get; set; }

        /// <summary>
        /// YAML docker compose etc
        /// </summary>
        public required string Plan { get; set; }
    }
}
