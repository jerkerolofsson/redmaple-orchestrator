using RedMaple.Orchestrator.Security.Models;
using System.Security.Cryptography.X509Certificates;

namespace RedMaple.Orchestrator.Security.Services
{
    public interface ICertificateAuthority
    {
        Task<X509Certificate2> GenerateCertificateAsync(CreateCertificateRequest request);

        /// <summary>
        /// Returns the intermediate CA certificate
        /// </summary>
        /// <returns></returns>
        Task<X509Certificate2> GetCaCertificateAsync();
        Task<IndexedCertificate> GetOrCreateCertificateAsync(string domainName);

        /// <summary>
        /// Returns the root certificate
        /// </summary>
        /// <returns></returns>
        Task<X509Certificate2> GetRootCertificateAsync();

        /// <summary>
        /// Replaces the root certificate and generates a new intermediate CA certificate
        /// </summary>
        /// <param name="cert"></param>
        /// <returns></returns>
        Task ReplaceRootCertificateAsync(X509Certificate2 cert);
    }
}