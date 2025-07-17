using RedMaple.Orchestrator.Contracts.Docker;

namespace RedMaple.Orchestrator.Node.Controllers
{
    public class DockerCertificatesController
    {
        private readonly ILogger<DockerCertificatesController> _logger;

        public DockerCertificatesController(ILogger<DockerCertificatesController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Returns all docker certificates on the host
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet("/api/docker-daemon/certificates")]
        public async Task<List<DockerCertificate>> GetDockerCertificatesAsync(CancellationToken cancellationToken)
        {
            var certs = new List<DockerCertificate>();
            if(OperatingSystem.IsLinux())
            {
                var certsd = new DirectoryInfo("/etc/docker/certs.d");
                if (certsd.Exists)
                {
                    foreach (var directoryInfo in certsd.GetDirectories())
                    {
                        var certName = directoryInfo.Name;
                        var caCrtFile = new FileInfo(Path.Combine(directoryInfo.FullName, "ca.crt"));
                        if (caCrtFile.Exists)
                        {
                            certs.Add(new DockerCertificate
                            {
                                RegistryDomain = certName,
                                CaCrt = await File.ReadAllTextAsync(caCrtFile.FullName)
                            });
                        }
                    }
                }
            }
            return certs;
        }

        /// <summary>
        /// Returns all docker certificates on the host
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost("/api/docker-daemon/certificates")]
        public async Task SetDockerCertificatesAsync([FromBody] List<DockerCertificate> certs, CancellationToken cancellationToken)
        {
            if (OperatingSystem.IsLinux())
            {
                var certsd = new DirectoryInfo("/etc/docker/certs.d");
                certsd.Create();

                foreach (var existingFolder in certsd.GetDirectories())
                {
                    try
                    {
                        existingFolder.Delete(true);
                    }
                    catch { }
                }

                foreach (var cert in certs)
                {
                    var certFolder = $"/etc/docker/certs.d/{cert.RegistryDomain}";
                    _logger.LogInformation("Writing certificate to {certFolder}", certFolder);
                    Directory.CreateDirectory(certFolder);

                    using var stream = File.CreateText(Path.Combine(certFolder, "ca.crt"));
                    await stream.WriteLineAsync(cert.CaCrt.ReplaceLineEndings());
                }
            }
        }
    }
}
