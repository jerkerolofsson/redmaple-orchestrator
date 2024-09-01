using RedMaple.Orchestrator.Security.Dtos;

namespace RedMaple.Orchestrator.Security.Models
{
    public class CreateCertificateRequest
    {
        /// <summary>
        /// Request name / app name. 
        /// This is mandatory and must be unique
        /// </summary>
        public string? Name { get; set; }

        public string? CommonNames { get; set; }
        public string? Organizations { get; set; }
        public string? OrganizationalUnits { get; set; }
        public string? Countries { get; set; }
        public string? Localities { get; set; }
        public string? StateOrProvinces { get; set; }

        public string? SanIpAddresses { get; set; }
        public string? SanDnsNames { get; set; }
        public string? IssuerSerial { get; set; }

        public double ExpiresInSeconds { get; set; }
        public List<EnhancedCertificateUsageDto> EnhancedUsages { get; set; } = new();

        /// <summary>
        /// If true, the cert will be self signed.
        /// If false it will be signed by a local certificate as specified by IssuerSerial
        /// </summary>
        public bool IsSelfSigned { get; set; }

        /// <summary>
        /// Signed by the local, intermediate, CA
        /// </summary>
        public bool SignByLocalCa { get; set; }

        /// <summary>
        /// Signed by the root CA
        /// </summary>
        public bool SignByRootCa { get; set; }

        public SignatureAlgorithmDto SignatureAlgorithm { get; set; }

        /// <summary>
        /// If true, this certificate is a CA that can sign certificates
        /// </summary>
        public bool IsCertificateAuthority { get; set; }

    }
}
