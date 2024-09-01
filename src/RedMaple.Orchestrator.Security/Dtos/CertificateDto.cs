namespace RedMaple.Orchestrator.Security.Dtos
{
    public class CertificateDto
    {
        public string? Name { get; set; }
        public string? Serial { get; set; }
        public string? Subject { get; set; }
        public string? IssuerName { get; set; }
        public List<CertificateUsageDto>? Usages { get; set; }
        public List<EnhancedCertificateUsageDto>? EnhancedUsages { get; set; }
        public DateTime NotAfter { get; set; }
        public DateTime NotBefore { get; set; }
        public string? SignatureAlgorithmName { get; set; }
        public string? Pkcs12Base64 { get; set; }
        public List<string>? SanDnsNames { get; set; }
        public List<string>? SanIpAddress { get; set; }
    }
}
