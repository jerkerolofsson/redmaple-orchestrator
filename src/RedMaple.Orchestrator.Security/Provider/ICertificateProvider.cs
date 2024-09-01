using RedMaple.Orchestrator.Security.Dtos;
using RedMaple.Orchestrator.Security.Models;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace RedMaple.Orchestrator.Security.Provider
{
    public interface ICertificateProvider
    {
        Task DeleteCertificateAsync(string certificateName);
        Task<CertificateDto?> ExportCertificateAsync(string certificateName, bool includePrivateData = false);
        Task<IndexedCertificate?> FindCertificateByNameAsync(string certificateName);
        Task<IndexedCertificate?> FindCertificateBySerialAsync(string serialNumber);
        Stream? GetCertificate(IndexedCertificate certificate, CertificateFileFormat fileFormat);
        Task<X509Certificate2> GetX509CertificateByNameAsync(string certificateName);
        Task SaveCertificateAsync(string name, X509Certificate2 certificate, List<X509Certificate2> chain);
    }
}