using Microsoft.AspNetCore.Mvc;
using RedMaple.Orchestrator.Contracts.Node;
using RedMaple.Orchestrator.Controller.Domain;
using RedMaple.Orchestrator.Sdk;
using RedMaple.Orchestrator.Security.Builder;
using RedMaple.Orchestrator.Security.Serialization;
using RedMaple.Orchestrator.Security.Services;
using System.IO;
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
            [FromQuery] string dnsName,
            [FromQuery] string? ipAddress,
            [FromQuery] string? password)
        {
            // Get intermediate
            var root = await _ca.GetRootCertificateAsync();
            var issuer = await _ca.GetCaCertificateAsync();

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
                        sanBuilder.AddDnsName(ipAddress);
                    }
                });

            if (password is not null)
            {
                builder.SetPassword(password);
            }

            var cert = builder.BuildSignedCertificate(issuer);
            var pfxBytes = CertificateSerializer.ToPfx(cert, password, [issuer, root]);

            var stream = new MemoryStream(pfxBytes);
            return File(stream, "text/plain", "cert.pfx");
        }
    }
}
