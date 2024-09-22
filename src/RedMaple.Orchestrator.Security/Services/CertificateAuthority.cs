using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.Security.Builder;
using RedMaple.Orchestrator.Security.Dtos;
using RedMaple.Orchestrator.Security.Models;
using RedMaple.Orchestrator.Security.Provider;
using RedMaple.Orchestrator.Security.Serialization;
using RedMaple.Orchestrator.Security.Storage;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace RedMaple.Orchestrator.Security.Services
{
    /// <summary>
    /// The certificate authority can generate certificates.
    /// 
    /// The following certificates are always created by default:
    /// 1. root-ca: Self signed certificate (can be changed with SaveRootCaCertificateAsync)
    /// 2. ca: Intermediate interface signed by root-ca (can be changed with SaveCaCertificateAsync)
    /// 
    /// Note that either of these certificates can be changed, so it is not guaranteede that the ca certificate is signed by the local root-ca
    /// </summary>
    public class CertificateAuthority : ICertificateAuthority
    {
        private readonly ILogger mLogger;
        private readonly ICertificateProvider mProvider;

        public CertificateAuthority(
            ILogger<CertificateAuthority> logger,
            ICertificateProvider provider)
        {
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(provider);

            mLogger = logger;
            mProvider = provider;
        }

        public async Task<X509Certificate2> GetCaCertificateAsync()
        {
            var certificate = await mProvider.FindCertificateByNameAsync("ca");
            if (certificate is null)
            {
                await GenerateCaCertificateAsync();
                certificate = await mProvider.FindCertificateByNameAsync("ca");
            }
            if (certificate?.Certificate is null)
            {
                throw new Exception("Failed to generate certificates");
            }
            return certificate.Certificate;
        }

        public async Task<X509Certificate2> GetRootCertificateAsync()
        {
            var certificate = await mProvider.FindCertificateByNameAsync("root");
            if (certificate is null)
            {
                var x509Certificate = await GenerateRootCertificateAsync();
                certificate = await mProvider.FindCertificateByNameAsync("root");
            }
            if (certificate?.Certificate is null)
            {
                throw new Exception("Failed to generate certificates");
            }
            return certificate.Certificate;
        }

        private async Task<X509Certificate2> GenerateRootCertificateAsync()
        {
            var builder = new CertificateBuilder()
                    .AddCommonName("redmaple_root_ca")
                    .AsCertificateAuthority()
                    .AddOrganization("Jerker Olofsson")
                    .AddLocality("Gothenburg")
                    .AddCountry("SE")
                    .WithExpiry(TimeSpan.FromDays(365 * 30))
                    .WithOid(CertificateBuilder.OID_TLS_WEB_SERVER_AUTHENTICATION)
                    .WithOid(CertificateBuilder.OID_TLS_WEB_CLIENT_AUTHENTICATION)
                    .WithOid(CertificateBuilder.OID_CODE_SIGNING)
                    .WithOid(CertificateBuilder.OID_EMAIL_PROTECTION);

            var cert = builder.BuildSelfSignedCertificate();
            await mProvider.SaveCertificateAsync("root", cert, []);
            return cert;
        }

        private async Task<X509Certificate2> GenerateCaCertificateAsync()
        {
            var builder = new CertificateBuilder()
                    .AddCommonName("redmaple_intermediate_ca")
                    .AsCertificateAuthority()
                    .AddOrganization("Jerker Olofsson")
                    .AddLocality("Gothenburg")
                    .AddCountry("SE")
                    .WithExpiry(TimeSpan.FromDays(365 * 30))
                    .WithOid(CertificateBuilder.OID_TLS_WEB_SERVER_AUTHENTICATION)
                    .WithOid(CertificateBuilder.OID_TLS_WEB_CLIENT_AUTHENTICATION)
                    .WithOid(CertificateBuilder.OID_CODE_SIGNING)
                    .WithOid(CertificateBuilder.OID_EMAIL_PROTECTION);

            var issuer = await GetRootCertificateAsync();
            var cert = builder.BuildSignedCertificate(issuer);
            await mProvider.SaveCertificateAsync("ca", cert, [issuer]);
            return cert;
        }

        public async Task<IndexedCertificate> GetOrCreateCertificateAsync(string domainName)
        {
            var cert = await mProvider.FindCertificateByNameAsync(domainName);
            if (cert is null)
            {
                await GenerateCertificateAsync(new CreateCertificateRequest
                {
                    SignByRootCa = true,
                    Name = domainName,
                    EnhancedUsages = new List<EnhancedCertificateUsageDto>
                    {
                        EnhancedCertificateUsageDto.ServerAuthentication
                    },
                    CommonNames = domainName,
                    SanDnsNames = domainName,
                    ExpiresInSeconds = TimeSpan.FromDays(365 * 5).TotalSeconds
                });
                cert = await mProvider.FindCertificateByNameAsync(domainName);

                if (cert is null)
                {
                    throw new Exception("Failed to generate certificate");
                }
            }
            return cert;
        }

        public async Task<X509Certificate2> GenerateCertificateAsync(CreateCertificateRequest request)
        {
            ArgumentNullException.ThrowIfNull(request?.Name);

            var chain = await GetCertificateChainForSigningAsync(request);
            if (chain.Count == 0)
            {
                // Build self signed
                var builder = CreateCertificateBuilder(request);
                var certificate = builder.BuildSelfSignedCertificate();
                await mProvider.SaveCertificateAsync(request.Name, certificate, chain);
                return certificate;
            }
            else
            {
                // Build signed
                var builder = CreateCertificateBuilder(request);
                var certificate = builder.BuildSignedCertificate(chain.First());
                await mProvider.SaveCertificateAsync(request.Name, certificate, chain);
                return certificate;
            }

        }

        private async Task<List<X509Certificate2>> GetCertificateChainForSigningAsync(CreateCertificateRequest request)
        {
            var root = await mProvider.FindCertificateByNameAsync("root");
            if (root?.Certificate is null)
            {
                await GenerateRootCertificateAsync();
                root = await mProvider.FindCertificateByNameAsync("root");
                if (root?.Certificate is null)
                {
                    throw new InvalidOperationException("root certificate not found");
                }
            }
            var ca = await mProvider.FindCertificateByNameAsync("ca");
            if (ca?.Certificate is null)
            {
                await GenerateCaCertificateAsync();
                ca = await mProvider.FindCertificateByNameAsync("ca");
                if (ca?.Certificate is null)
                {
                    throw new InvalidOperationException("ca certificate not found");
                }
            }

            var chain = new List<X509Certificate2>();
            if (!request.IsSelfSigned)
            {
                if (request.SignByRootCa)
                {
                    chain.Add(root!.Certificate);
                }
                else if (request.SignByLocalCa)
                {
                    chain.Add(ca!.Certificate);
                    chain.Add(root!.Certificate);
                }
                else
                {
                    if (request.IssuerSerial is null)
                    {
                        throw new InvalidOperationException("No Issuer specified");
                    }

                    // Find the issuer certificate to use for signing
                    var issuer = await mProvider.FindCertificateBySerialAsync(request.IssuerSerial);
                    if (issuer?.Certificate is null)
                    {
                        throw new Exception("Issuer not found");
                    }
                    chain.Add(issuer.Certificate);
                }
            }
            return chain;
        }

        internal X509Certificate2 GenerateSelfSignedCertificate(CertificateBuilder builder)
        {
            var x509Certificate = builder.BuildSelfSignedCertificate();

            return x509Certificate;
        }


        private CertificateBuilder CreateCertificateBuilder(CreateCertificateRequest request)
        {
            var builder = new CertificateBuilder();

            switch (request.SignatureAlgorithm)
            {
                case SignatureAlgorithmDto.ECDsa:
                    builder.UseECDsa();
                    break;
                case SignatureAlgorithmDto.Rsa:
                    builder.UseRSA();
                    break;
                default:
                    throw new NotImplementedException();
            }

            builder.WithExpiry(TimeSpan.FromSeconds(request.ExpiresInSeconds));

            if (request.SanIpAddresses != null || request.SanDnsNames != null)
            {
                builder.WithSubjectAlternativeName((san) =>
                {
                    if (!string.IsNullOrEmpty(request.SanIpAddresses))
                    {
                        foreach (var attributeValue in request.SanIpAddresses.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        {
                            san.AddIpAddress(IPAddress.Parse(attributeValue));
                        }
                    }
                    if (!string.IsNullOrEmpty(request.SanDnsNames))
                    {
                        foreach (var attributeValue in request.SanDnsNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        {
                            san.AddDnsName(attributeValue);
                        }
                    }
                });
            }

            if (request.CommonNames != null)
            {
                foreach (var attributeValue in request.CommonNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    builder.AddCommonName(attributeValue);
                }
            }
            if (request.StateOrProvinces != null)
            {
                foreach (var attributeValue in request.StateOrProvinces.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    builder.AddStateOrProvince(attributeValue);
                }
            }
            if (request.Localities != null)
            {
                foreach (var attributeValue in request.Localities.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    builder.AddLocality(attributeValue);
                }
            }
            if (request.Countries != null)
            {
                foreach (var attributeValue in request.Countries.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    builder.AddCountry(attributeValue);
                }
            }
            if (request.OrganizationalUnits != null)
            {
                foreach (var attributeValue in request.OrganizationalUnits.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    builder.AddOrganizationalUnit(attributeValue);
                }
            }
            if (request.Organizations != null)
            {
                foreach (var attributeValue in request.Organizations.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    builder.AddOrganization(attributeValue);
                }
            }
            if (request.EnhancedUsages != null)
            {
                foreach (var usage in request.EnhancedUsages)
                {
                    switch (usage)
                    {
                        case EnhancedCertificateUsageDto.ServerAuthentication:
                            builder.WithOid(CertificateBuilder.OID_TLS_WEB_SERVER_AUTHENTICATION);
                            break;
                        case EnhancedCertificateUsageDto.ClientAuthentication:
                            builder.WithOid(CertificateBuilder.OID_TLS_WEB_CLIENT_AUTHENTICATION);
                            break;
                        case EnhancedCertificateUsageDto.EmailProtection:
                            builder.WithOid(CertificateBuilder.OID_EMAIL_PROTECTION);
                            break;
                        case EnhancedCertificateUsageDto.CodeSigning:
                            builder.WithOid(CertificateBuilder.OID_CODE_SIGNING);
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
            if (request.IsCertificateAuthority)
            {
                builder.AsCertificateAuthority();
            }

            return builder;
        }

        public async Task ReplaceRootCertificateAsync(X509Certificate2 cert)
        {
            await mProvider.DeleteCertificateAsync("root");
            await mProvider.DeleteCertificateAsync("ca");

            await mProvider.SaveCertificateAsync("root", cert, []);

            // Generate CA certificate
            await GetCaCertificateAsync();
        }
    }
}
