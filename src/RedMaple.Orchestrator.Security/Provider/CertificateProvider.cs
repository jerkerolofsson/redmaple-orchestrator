using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.Security.Dtos;
using RedMaple.Orchestrator.Security.Models;
using RedMaple.Orchestrator.Security.Serialization;
using RedMaple.Orchestrator.Security.Services;
using RedMaple.Orchestrator.Security.Storage;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;

namespace RedMaple.Orchestrator.Security.Provider
{
    public class CertificateProvider : ICertificateProvider
    {
        private readonly ILogger<CertificateProvider> mLogger;
        private string mCaRootDir;

        public CertificateProvider(
            ILogger<CertificateProvider> logger,
            ICertificateFileStorage fileStorage)
        {
            mLogger = logger;
            mCaRootDir = fileStorage.GetRootDirectory();
            if(!Directory.Exists(mCaRootDir))
            { 
                Directory.CreateDirectory(mCaRootDir); 
            }
        }

        public Task SaveCertificateAsync(string name, X509Certificate2 certificate, List<X509Certificate2> chain)
        {
            var path = Path.Combine(mCaRootDir, name);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            CertificateSerializer.ToPfxFile(certificate, Path.Combine(path, "cert.pfx"), chain);
            CertificateSerializer.ToCertFile(certificate, Path.Combine(path, "cert.crt"), chain);
            CertificateSerializer.ToPemCertFile(certificate, Path.Combine(path, "cert.pem"), chain);
            CertificateSerializer.ToPemKeyFile(certificate, Path.Combine(path, "key.pem"));
            return Task.CompletedTask;
        }

        public Task<CertificateDto?> ExportCertificateAsync(string name, bool includePrivateData)
        {
            var dto = FindCertificates().Where(x => x.Name == name).Select(x => x.ToDto(includePrivateData: includePrivateData)).FirstOrDefault();
            return Task.FromResult(dto);
        }

        public Task<IndexedCertificate?> FindCertificateByNameAsync(string name)
        {
            IndexedCertificate? certificate = null;
            foreach (var cert in FindCertificates())
            {
                if (cert.Name == name)
                {
                    certificate = cert;
                    break;
                }
            }
            return Task.FromResult(certificate);
        }
        public Task<IndexedCertificate?> FindCertificateBySerialAsync(string serialNumber)
        {
            IndexedCertificate? certificate = null;
            foreach (var cert in FindCertificates())
            {
                if (cert.Certificate?.GetSerialNumberString() == serialNumber)
                {
                    certificate = cert;
                    break;
                }
            }
            return Task.FromResult(certificate);
        }

        public Task DeleteCertificateAsync(string certificateName)
        {
            foreach (var cert in FindCertificates())
            {
                if (cert.Name == certificateName)
                {
                    if (!string.IsNullOrEmpty(cert.DirectoryPath))
                    {
                        var files = new DirectoryInfo(cert.DirectoryPath).GetFiles("*", SearchOption.AllDirectories);
                        if (files.Length < 10) // Safety check, should be around ~5 files
                        {
                            new DirectoryInfo(cert.DirectoryPath).Delete(true);
                        }
                    }
                }
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Scans the root directory for all certificates
        /// </summary>
        /// <returns></returns>
        private IEnumerable<IndexedCertificate> FindCertificates()
        {
            if (mCaRootDir is null)
            {
                throw new InvalidOperationException("Not initialized");
            }

            var dirInfo = new DirectoryInfo(mCaRootDir);
            foreach (var dir in dirInfo.GetDirectories())
            {
                var pfx = Path.Combine(dir.FullName, "cert.pfx");
                var crt = Path.Combine(dir.FullName, "cert.crt");
                var key = Path.Combine(dir.FullName, "key.pem");
                var pem = Path.Combine(dir.FullName, "cert.pem");
                if (File.Exists(pfx))
                {
                    string? password = null;
                    var cert = new X509Certificate2(pfx, password, X509KeyStorageFlags.Exportable);
                    var name = dir.Name;

                    yield return new IndexedCertificate()
                    {
                        DirectoryPath = dir.FullName,
                        PfxPath = pfx,
                        CrtPath = crt,
                        PemCertPath = pem,
                        PemKeyPath = key,
                        Certificate = cert,
                        Name = name
                    };
                }
            }
        }

        /// <summary>
        /// Returns a stream for the serialized certificate content (if it exists).
        /// If not, null is returned;
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="fileFormat"></param>
        /// <returns></returns>
        public Stream? GetCertificate(IndexedCertificate certificate, CertificateFileFormat fileFormat)
        {
            string? filename = null;
            switch (fileFormat)
            {
                case CertificateFileFormat.KeyPem:
                    filename = certificate.PemKeyPath;
                    break;
                case CertificateFileFormat.Crt:
                    filename = certificate.CrtPath;
                    break;
                case CertificateFileFormat.Pfx:
                    filename = certificate.PfxPath;
                    break;
                case CertificateFileFormat.CertPem:
                    filename = certificate.PemCertPath;
                    break;
            }
            if (filename != null && File.Exists(filename))
            {
                return File.OpenRead(filename);
            }
            return null;
        }

        public async Task<X509Certificate2> GetX509CertificateByNameAsync(string certificateName)
        {
            var indexedCert = await FindCertificateByNameAsync(certificateName);
            if(indexedCert?.Certificate is null)
            {
                throw new Exception("Certificate not found");
            }
            return indexedCert.Certificate;
        }
    }
}
