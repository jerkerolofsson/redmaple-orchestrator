using RedMaple.Orchestrator.Security.Builder;
using RedMaple.Orchestrator.Security.Dtos;
using RedMaple.Orchestrator.Security.Serialization;
using System.Security.Cryptography.X509Certificates;

namespace RedMaple.Orchestrator.Security.Models
{
    /// <summary>
    /// Represents a collection of formats for a specific certificate, on disk
    /// </summary>
    public class IndexedCertificate
    {
        public X509Certificate2? Certificate { get; set; }
        public string? Name { get; set; }
        public string? PfxPath { get; internal set; }
        public string? Password { get; internal set; }
        public string? CrtPath { get; internal set; }
        public string? PemCertPath { get; internal set; }
        public string? PemKeyPath { get; internal set; }
        public string? DirectoryPath { get; internal set; }

        public CertificateDto ToDto(bool includePrivateData)
        {
            if (Certificate is null)
            {
                throw new Exception("Certificate is null");
            }

            var usages = new List<CertificateUsageDto>();
            var enhancedUsages = new List<EnhancedCertificateUsageDto>();

            foreach (var x509KeyUsageExtension in Certificate.Extensions.OfType<X509KeyUsageExtension>())
            {
                if ((x509KeyUsageExtension.KeyUsages & X509KeyUsageFlags.KeyCertSign) == X509KeyUsageFlags.KeyCertSign)
                {
                    usages.Add(CertificateUsageDto.CertificateSigning);
                }
                if ((x509KeyUsageExtension.KeyUsages & X509KeyUsageFlags.CrlSign) == X509KeyUsageFlags.CrlSign)
                {
                    usages.Add(CertificateUsageDto.CrlSigning);
                }
                if ((x509KeyUsageExtension.KeyUsages & X509KeyUsageFlags.DigitalSignature) == X509KeyUsageFlags.DigitalSignature)
                {
                    usages.Add(CertificateUsageDto.DigitalSignature);
                }
                if ((x509KeyUsageExtension.KeyUsages & X509KeyUsageFlags.KeyAgreement) == X509KeyUsageFlags.KeyAgreement)
                {
                    usages.Add(CertificateUsageDto.KeyAgreement);
                }
                if ((x509KeyUsageExtension.KeyUsages & X509KeyUsageFlags.KeyEncipherment) == X509KeyUsageFlags.KeyEncipherment)
                {
                    usages.Add(CertificateUsageDto.KeyEncipherment);
                }
                if ((x509KeyUsageExtension.KeyUsages & X509KeyUsageFlags.DataEncipherment) == X509KeyUsageFlags.DataEncipherment)
                {
                    usages.Add(CertificateUsageDto.DataEncipherment);
                }
                if ((x509KeyUsageExtension.KeyUsages & X509KeyUsageFlags.DecipherOnly) == X509KeyUsageFlags.DecipherOnly)
                {
                    usages.Add(CertificateUsageDto.DecipherOnly);
                }
                if ((x509KeyUsageExtension.KeyUsages & X509KeyUsageFlags.EncipherOnly) == X509KeyUsageFlags.EncipherOnly)
                {
                    usages.Add(CertificateUsageDto.EncipherOnly);
                }
            }
            foreach (var x509EnhancedKeyUsageExtension in Certificate.Extensions.OfType<X509EnhancedKeyUsageExtension>())
            {
                foreach (var oid in x509EnhancedKeyUsageExtension.EnhancedKeyUsages)
                {
                    if (oid.Value == CertificateBuilder.OID_CODE_SIGNING)
                    {
                        enhancedUsages.Add(EnhancedCertificateUsageDto.CodeSigning);
                    }
                    else if (oid.Value == CertificateBuilder.OID_EMAIL_PROTECTION)
                    {
                        enhancedUsages.Add(EnhancedCertificateUsageDto.EmailProtection);
                    }
                    else if (oid.Value == CertificateBuilder.OID_TLS_WEB_SERVER_AUTHENTICATION)
                    {
                        enhancedUsages.Add(EnhancedCertificateUsageDto.ServerAuthentication);
                    }
                    else if (oid.Value == CertificateBuilder.OID_TLS_WEB_CLIENT_AUTHENTICATION)
                    {
                        enhancedUsages.Add(EnhancedCertificateUsageDto.ClientAuthentication);
                    }
                }
            }

            var dto = new CertificateDto()
            {
                Name = Name,
                IssuerName = Certificate.Issuer,
                Serial = Certificate.GetSerialNumberString(),
                Subject = Certificate.Subject,
                Usages = usages,
                EnhancedUsages = enhancedUsages,
                NotBefore = Certificate.NotBefore,
                NotAfter = Certificate.NotAfter,
                SignatureAlgorithmName = Certificate.SignatureAlgorithm.FriendlyName
            };

            dto.SanDnsNames = new List<string>();
            dto.SanIpAddress = new List<string>();
            foreach (var x509KeyUsageExtension in Certificate.Extensions.OfType<X509SubjectAlternativeNameExtension>())
            {
                foreach (var dnsName in x509KeyUsageExtension.EnumerateDnsNames())
                {
                    dto.SanDnsNames.Add(dnsName);
                }
                foreach (var ipAddress in x509KeyUsageExtension.EnumerateIPAddresses())
                {
                    dto.SanIpAddress.Add(ipAddress.ToString());
                }
            }

            if (includePrivateData)
            {
                dto.Pkcs12Base64 = Convert.ToBase64String(CertificateSerializer.ToPkcs12(Certificate));

            }
            return dto;
        }
    }
}
