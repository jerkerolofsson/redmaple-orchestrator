using RedMaple.Orchestrator.Security.Models;
using System.Security.Cryptography.X509Certificates;

namespace RedMaple.Orchestrator.Security.Services
{
    public interface ICertificateAuthority
    {
        Task<X509Certificate2> GenerateCertificateAsync(CreateCertificateRequest request);
        Task<X509Certificate2> GetCaCertificateAsync();
        Task<IndexedCertificate> GetOrCreateCertificateAsync(string domainName);
        Task<X509Certificate2> GetRootCertificateAsync();
    }
}