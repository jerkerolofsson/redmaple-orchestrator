using Docker.DotNet.Models;
using RedMaple.Orchestrator.Containers;
using RedMaple.Orchestrator.Contracts;
using RedMaple.Orchestrator.Security.Provider;
using RedMaple.Orchestrator.Security.Services;
using System.Runtime.InteropServices;
using System.Text;

namespace RedMaple.Orchestrator.Ingress
{
    public class Nginx : IReverseProxy
    {
        private readonly ICertificateProvider _certificateProvider;
        private readonly ILocalContainersClient _localContainersClient;
        private readonly CertificateAuthority _certificateAuthority;

        public Nginx(ILocalContainersClient localContainersClient, CertificateAuthority certificateAuthority, ICertificateProvider certificateProvider)
        {
            _localContainersClient = localContainersClient;
            _certificateAuthority = certificateAuthority;
            _certificateProvider = certificateProvider;
        }

        public async Task UpdateConfigurationAsync(List<IngressServiceDescription> services)
        {
            var conf = new StringBuilder();
            foreach (var service in services)
            {
                var certificate = await _certificateAuthority.GetOrCreateCertificateAsync(service.DomainName);
                if (certificate.PemKeyPath is null)
                {
                    throw new Exception("key error");
                }
                if (certificate.PemCertPath is null)
                {
                    throw new Exception("certificate error");
                }

                var pemKeyName = Path.Combine(GetLocalConfigDir(), service.DomainName + ".key");
                var pemCertName = Path.Combine(GetLocalConfigDir(), service.DomainName + ".crt");
                File.Copy(certificate.PemKeyPath, pemKeyName, true);
                File.Copy(certificate.PemCertPath, pemCertName, true);

                conf.AppendLine("  server {");
                if (service.Schema == "https")
                {
                    conf.AppendLine($"    listen  {service.IngressPort} ssl;");
                }
                else
                {
                    conf.AppendLine($"    listen  {service.IngressPort};");
                }
                conf.AppendLine($"    server_name " + service.DomainName + ";");
                conf.AppendLine($"    ssl_certificate /etc/nginx/conf.d/" + service.DomainName + ".crt;");
                conf.AppendLine($"    ssl_certificate_key /etc/nginx/conf.d/" + service.DomainName + ".key;");
                conf.AppendLine("    location / {");
                conf.AppendLine($"       proxy_pass {service.Schema}://{service.DestinationIp}:{service.DestinationPort};");
                conf.AppendLine("    }");
                conf.AppendLine("  }");
            }
            await File.WriteAllTextAsync(GetLocalConfigPath(), conf.ToString());

            await StopAsync();
            await StartAsync();
        }

        private string GetLocalConfigDir()
        {
            string path = "/var/redmaple/nginx";
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (isWindows)
            {
                path = @"C:\temp\nginx";
            }
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }
        private string GetLocalConfigPath()
        {
            string path = GetLocalConfigDir();
            var filePath = Path.Combine(path, "nginx.conf");

            if (!File.Exists(filePath))
            {
                File.Create(filePath).Close();
            }
            return filePath;
        }

        public async Task StartAsync()
        {
            var name = "redmaple-ingress";
            bool result = await _localContainersClient.HasContainerByNameAsync(name);
            if (!result)
            {
                var configFile = GetLocalConfigPath();
                

                await _localContainersClient.CreateContainerAsync(new CreateContainerParameters
                {
                    Name = "redmaple-ingress",
                    Image = "nginx:latest",
                    ExposedPorts = new Dictionary<string, EmptyStruct>
                    {
                        { "8080/tcp", new EmptyStruct() },
                        { "8443/tcp", new EmptyStruct() }
                    },
                    HostConfig = new HostConfig
                    {
                        Binds = new List<string> { $"{GetLocalConfigDir()}:/etc/nginx/conf.d/" },
                        PortBindings = new Dictionary<string, IList<PortBinding>>
                        {
                            { "8080/tcp", new List<PortBinding>() { new PortBinding() { HostPort = "8080" } } },
                            { "8443/tcp", new List<PortBinding>() { new PortBinding() { HostPort = "8443" } } }
                        }
                    }
                });
            }

            var container = await _localContainersClient.GetContainerByNameAsync(name);
            if (container is null)
            {
                throw new Exception("Failed to create container");
            }
            if (!container.IsRunning)
            {
                await _localContainersClient.StartAsync(container.Id);
            }
        }

        public async Task StopAsync()
        {
            var name = "redmaple-ingress";
            var container = await _localContainersClient.GetContainerByNameAsync(name);
            if (container is not null)
            {
                if (container.IsRunning)
                {
                    await _localContainersClient.StopAsync(container.Id);
                }
            }
        }
    }
}
