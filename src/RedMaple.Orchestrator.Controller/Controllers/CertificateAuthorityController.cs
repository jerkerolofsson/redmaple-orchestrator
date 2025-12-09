using Microsoft.AspNetCore.Mvc;
using RedMaple.Orchestrator.Contracts.Node;
using RedMaple.Orchestrator.Controller.Domain;
using RedMaple.Orchestrator.Sdk;
using RedMaple.Orchestrator.Security.Builder;
using RedMaple.Orchestrator.Security.Serialization;
using RedMaple.Orchestrator.Security.Services;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace RedMaple.Orchestrator.Controller.Controllers
{
    [Route("api/ca")]
    [ApiController]
    public class CertificateAuthorityController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly ICertificateAuthority _ca;

        public CertificateAuthorityController(
            ILogger<CertificateAuthorityController> logger,
            ICertificateAuthority certificateAuthority)
        {
            _logger = logger;
            _ca = certificateAuthority;
        }

        [HttpGet("/api/ca/intermediate/cert")]
        public async Task<IActionResult> GetIntermediateCertificateAsync()
        {
            var cert = await _ca.GetCaCertificateAsync();
            var certBytes = cert.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Cert);

            var stream = new MemoryStream(certBytes);
            return File(stream, "application/octet-stream", "intermediate_ca.crt");
        }
        [HttpGet("/api/ca/intermediate/pem")]
        public async Task<IActionResult> GetIntermediateCertificateAsPemAsync()
        {
            var cert = await _ca.GetCaCertificateAsync();
            var pemText = cert.ExportCertificatePem();

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(pemText));
            return File(stream, "text/plain", "intermediate_ca.pem");
        }

        /// <summary>
        /// Replaces the root certificate
        /// </summary>
        /// <param name="pfxBytes"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [HttpPost("/api/ca/root")]
        public async Task<IActionResult> ReplaceRootCertificateAsync(
            [FromBody] byte[] pfxBytes,
            [FromQuery] string? password)
        {

            var cert = new X509Certificate2(pfxBytes, password, X509KeyStorageFlags.Exportable);
            await _ca.ReplaceRootCertificateAsync(cert);

            return Ok();
        }

        [HttpGet("/api/ca/root/cert")]
        public async Task<IActionResult> GetRootCertificateAsync()
        {
            var cert = await _ca.GetRootCertificateAsync();
            var certBytes = cert.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Cert);

            var stream = new MemoryStream(certBytes);
            return File(stream, "application/octet-stream", "root_ca.crt");
        }
        [HttpGet("/api/ca/root/pem")]
        public async Task<IActionResult> GetRootCertificateAsPemAsync()
        {
            var cert = await _ca.GetRootCertificateAsync();
            var pemText = cert.ExportCertificatePem();

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(pemText));
            return File(stream, "text/plain", "root_ca.pem");
        }

        [HttpGet("/api/ca/generate")]
        public async Task<IActionResult> GenerateCertificateAsync(
            [FromQuery] string? cn,
            [FromQuery] string? dnsName,
            [FromQuery] string? ipAddress,
            [FromQuery] string? password,
            [FromQuery] string? format)
        {
            // Get intermediate
            var root = await _ca.GetRootCertificateAsync();
            var issuer = await _ca.GetCaCertificateAsync();

            if (string.IsNullOrEmpty(cn))
            {
                cn = null;
            }
            if (string.IsNullOrEmpty(dnsName))
            {
                dnsName = null;
            }
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = null;
            }

            var name = cn ?? dnsName ?? "certificate";

            var builder = new CertificateBuilder()
                .AddCommonName(name)
                .WithExpiry(TimeSpan.FromDays(365*3))
                .WithOid(CertificateBuilder.OID_TLS_WEB_SERVER_AUTHENTICATION)
                .WithSubjectAlternativeName((sanBuilder) =>
                {
                    if (dnsName is not null)
                    {
                        sanBuilder.AddDnsName(dnsName);
                    }
                    if(ipAddress is not null)
                    {
                        sanBuilder.AddIpAddress(IPAddress.Parse(ipAddress));
                    }
                });

            if (password is not null)
            {
                builder.SetPassword(password);
            }

            var cert = builder.BuildSignedCertificate(issuer);
            var filename = dnsName ?? "cert";

            if (format == "pfx")
            {
                var stream = new MemoryStream(CertificateSerializer.ToPfx(cert, password, [issuer, root]));
                return File(stream, "text/plain", $"{filename}.pfx");
            }
            else
            {
                var pfx = CertificateSerializer.ToPfx(cert, password, [issuer, root]);

                var rootCaCrtBytes = CertificateSerializer.ToCert(root);
                var intermediateCaCrtBytes = CertificateSerializer.ToCert(issuer);

                var pksc7Bytes = CertificateSerializer.ExportCertificateChainAsByteArray(X509ContentType.Pkcs7, cert);
                var pksc12Bytes = CertificateSerializer.ExportCertificateChainAsByteArray(X509ContentType.Pkcs12, cert);
                var certBytes = CertificateSerializer.ToCert(cert);
                var certPems = CertificateSerializer.ExportCertificatePemsAsByteArray(cert, issuer, root); // nginx order
                var certPemsReverse = CertificateSerializer.ExportCertificatePemsAsByteArray(cert, issuer, root); // nginx order
                var keyPem = CertificateSerializer.ExportPrivateKeyToPem(cert);

                var stream = new MemoryStream();
                using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen:true))
                {
                    var pfxEntry = zip.CreateEntry("cert.pfx");
                    using (var entryStream = pfxEntry.Open())
                    {
                        await entryStream.WriteAsync(pfx);
                    }
                    var certPemsEntry = zip.CreateEntry("cert.pem");
                    using (var entryStream = certPemsEntry.Open())
                    {
                        await entryStream.WriteAsync(certPems);
                    }
                    var certPemsReverseEntry = zip.CreateEntry("cert_chain_reverse.pem");
                    using (var entryStream = certPemsReverseEntry.Open())
                    {
                        await entryStream.WriteAsync(certPemsReverse);
                    }
                    var keyEntry = zip.CreateEntry("key.pem");
                    using (var entryStream = keyEntry.Open())
                    {
                        await entryStream.WriteAsync(keyPem);
                    }
                    var crtEntry = zip.CreateEntry("cert.crt");
                    using (var entryStream = crtEntry.Open())
                    {
                        await entryStream.WriteAsync(certBytes);
                    }
                    var pksc7 = zip.CreateEntry("cert.pksc7");
                    using (var entryStream = pksc7.Open())
                    {
                        await entryStream.WriteAsync(pksc7Bytes);
                    }
                    var pksc12 = zip.CreateEntry("cert.pksc12");
                    using (var entryStream = pksc12.Open())
                    {
                        await entryStream.WriteAsync(pksc12Bytes);
                    }

                    var rootCaEntry = zip.CreateEntry("root_ca.crt");
                    using (var entryStream = rootCaEntry.Open())
                    {
                        await entryStream.WriteAsync(rootCaCrtBytes);
                    }
                    var intermediateCaEntry = zip.CreateEntry("intermediate_ca.crt");
                    using (var entryStream = intermediateCaEntry.Open())
                    {
                        await entryStream.WriteAsync(intermediateCaCrtBytes);
                    }
                }
                stream.Seek(0, SeekOrigin.Begin);
                return File(stream, "application/zip", $"{filename}.zip");
            }
        }
    }
}
