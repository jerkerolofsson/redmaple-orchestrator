using RedMaple.Orchestrator.Security.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Security.Models
{
    internal class CertificateBuilderData
    {
        public X509KeyUsageFlags Usages { get; set; } = X509KeyUsageFlags.KeyAgreement | X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature;
        public List<string> Oids { get; set; } = new();
        public bool IsCertificateAuthority { get; set; } = false;

        public string? FriendlyName { get; set; }

        public TimeSpan Expiry { get; set; } = TimeSpan.FromDays(365);

        public DateTime ValidityStartTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// OU
        /// </summary>
        public List<string> OrganizationalUnits { get; set; } = new();

        /// <summary>
        /// O
        /// </summary>
        public List<string> Organizations { get; set; } = new();

        /// <summary>
        /// C
        /// </summary>
        public List<string> Countries { get; set; } = new();

        /// <summary>
        /// L
        /// </summary>
        public List<string> Localities { get; set; } = new();

        /// <summary>
        /// S
        /// </summary>
        public List<string> StateOrProvinceNames { get; set; } = new();

        /// <summary>
        /// CN
        /// </summary>
        public List<string> CommonNames { get; set; } = new();

        /// <summary>
        /// Password, for encryption
        /// </summary>
        public string? Password { get; set; }
        public SignatureAlgorithm SignatureAlgorithm { get; set; }

    }
}
