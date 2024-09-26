using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.Contracts.Containers;
using RedMaple.Orchestrator.Contracts.Ingress;
using System.Runtime.InteropServices;
using System.Text;

namespace RedMaple.Orchestrator.Ingress
{
    public class Nginx : IReverseProxy
    {
        private readonly ILogger _logger;
        private readonly ILocalContainersClient _localContainersClient;

        public Nginx(
            ILogger<Nginx> logger,
            ILocalContainersClient localContainersClient)
        {
            _logger = logger;
            _localContainersClient = localContainersClient;
        }


        private async Task CreateCommonConfIfNotExistsAsync()
        {
            await CreateCommonServerIfNotExistsAsync();
            await CreateCommonLocationIfNotExistsAsync();
        }

        private async Task CreateCommonServerIfNotExistsAsync()
        {
            var path = Path.Combine(GetConfdConfigDir(), "common_server.conf");
            if (!File.Exists(path))
            {
                var conf = new StringBuilder();
                conf.Append("""
                client_max_body_size 300M;
                ssl_session_cache    shared:SSL:1m;
                ssl_session_timeout  5m;
                """);

                await File.WriteAllTextAsync(path, conf.ToString());
            }
        }

        private async Task CreateCommonLocationIfNotExistsAsync()
        {
            var path = Path.Combine(GetConfdConfigDir(), "common_location.conf");
            if (!File.Exists(path))
            {
                var conf = new StringBuilder();
                conf.Append("""
                proxy_redirect     off;
                proxy_set_header Host $host;
                """);

                await File.WriteAllTextAsync(path, conf.ToString());
            }
        }
        private async Task CreateNginxMainConfAsync()
        {
            var path = GetLocalConfigPath();
            _logger.LogInformation("Creating nginx.conf @ {path}", path);
            var conf = new StringBuilder();
            conf.Append("""
                user  nginx;
                worker_processes  auto;

                error_log  /var/log/nginx/error.log notice;
                pid        /var/run/nginx.pid;


                events {
                    worker_connections  1024;
                }

                http {
                    include       /etc/nginx/mime.types;
                    default_type  application/octet-stream;

                    log_format  main  '$remote_addr - $remote_user [$time_local] "$request" '
                                      '$status $body_bytes_sent "$http_referer" '
                                      '"$http_user_agent" "$http_x_forwarded_for"';

                    access_log  /var/log/nginx/access.log  main;

                    server_names_hash_bucket_size 64;
                    proxy_headers_hash_max_size 1024;
                    proxy_headers_hash_bucket_size 128;
                    proxy_buffer_size   128k;
                    proxy_buffers   4 256k;
                    proxy_busy_buffers_size   256k;
                    large_client_header_buffers  4 32k;

                    sendfile        on;
                    #tcp_nopush     on;

                    keepalive_timeout  65;

                    gzip  on;

                    include /etc/nginx/conf.d/*.conf;
                }
                """);

            await File.WriteAllTextAsync(path, conf.ToString());

        }

        public async Task UpdateConfigurationAsync(List<IngressServiceDescription> services)
        {
            var conf = new StringBuilder();
            foreach (var service in services)
            {
                if (service.PemKey is null)
                {
                    throw new Exception("key error");
                }
                if (service.PemCert is null)
                {
                    throw new Exception("certificate error");
                }

                var pemKeyName = Path.Combine(GetConfdConfigDir(), service.DomainName + ".key");
                var pemCertName = Path.Combine(GetConfdConfigDir(), service.DomainName + ".crt");
                File.WriteAllBytes(pemKeyName, service.PemKey);
                File.WriteAllBytes(pemCertName, service.PemCert);

                conf.AppendLine($$"""
                    
                    server {
                        listen 80;
                        server_name {{service.DomainName}};
                        return 301 https://{{service.DomainName}}$request_uri;
                    }                    
                    server {
                        listen {{service.IngressPort}} ssl;
                        server_name {{service.DomainName}};
                        ssl_certificate /etc/nginx/conf.d/{{service.DomainName}}.crt;
                        ssl_certificate_key /etc/nginx/conf.d/{{service.DomainName}}.key;
                        include /etc/nginx/conf.d/common_server.conf;
                    
                        location / {
                            proxy_pass {{service.Scheme}}://{{service.DestinationIp}}:{{service.DestinationPort}};
                            include /etc/nginx/conf.d/common_location.conf;
                        }
                    }
                    """);
            }
            await File.WriteAllTextAsync(GetServersConfigPath(), conf.ToString());

            await StopAsync();
            await StartAsync();
        }

        private string GetConfdConfigDir()
        {
            string path = "/data/redmaple/node/nginx/conf.d";
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (isWindows)
            {
                path = @"C:\temp\redmaple\node\nginx\conf.d";
            }
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }
        private string GetLocalConfigDir()
        {
            string path = "/data/redmaple/node/nginx";
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (isWindows)
            {
                path = @"C:\temp\redmaple\node\nginx";
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
        private string GetServersConfigPath()
        {
            string path = GetConfdConfigDir();
            var filePath = Path.Combine(path, "servers.conf");

            if (!File.Exists(filePath))
            {
                File.Create(filePath).Close();
            }
            return filePath;
        }

        public async Task StartAsync()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            await CreateNginxMainConfAsync();
            await CreateCommonConfIfNotExistsAsync();

            var name = "redmaple-ingress";
            bool result = await _localContainersClient.HasContainerByNameAsync(name, cts.Token);
            if (!result)
            {
                var configFile = GetServersConfigPath();

                string httpPort = Environment.GetEnvironmentVariable("NGINX_HTTP_PORT") ?? "80";
                string httpsPort = Environment.GetEnvironmentVariable("NGINX_HTTPS_PORT") ?? "443";

                using var createCts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
                await _localContainersClient.CreateContainerAsync(new CreateContainerParameters
                {
                    Name = "redmaple-ingress",
                    Image = "nginx:latest",
                    ExposedPorts = new Dictionary<string, EmptyStruct>
                    {
                        { "80/tcp", new EmptyStruct() },
                        { "443/tcp", new EmptyStruct() }
                    },
                    HostConfig = new HostConfig
                    {
                        Binds = new List<string> 
                        {
                            $"{GetLocalConfigPath()}:/etc/nginx/nginx.conf",
                            $"{GetConfdConfigDir()}:/etc/nginx/conf.d/" 
                        },
                        PortBindings = new Dictionary<string, IList<PortBinding>>
                        {
                            { "80/tcp", new List<PortBinding>() { new PortBinding() { HostPort = httpPort } } },
                            { "443/tcp", new List<PortBinding>() { new PortBinding() { HostPort = httpsPort } } }
                        }
                    }
                }, createCts.Token);
            }

            var container = await _localContainersClient.GetContainerByNameAsync(name, cts.Token);
            if (container is null)
            {
                throw new Exception("Failed to create container");
            }

            _logger.LogInformation("StartAsync: nginx container id={id}, state={state}, status={status}", container.Id, container.State, container.Status);
            if (!container.IsRunning)
            {
                _logger.LogInformation("StartAsync: Starting nginx");
                using var ctsStart = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await _localContainersClient.StartAsync(container.Id, ctsStart.Token);
            }
            else
            {
                _logger.LogInformation("StartAsync: nginx is already running");
            }
        }

        public async Task StopAsync()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));

            var name = "redmaple-ingress";
            var container = await _localContainersClient.GetContainerByNameAsync(name, cts.Token);
            if (container is not null)
            {
                _logger.LogInformation("StopAsync: nginx container id={id}, state={state}, status={status}", container.Id, container.State, container.Status);
                if (container.IsRunning)
                {
                    _logger.LogInformation("StopAsync: Stopping nginx");
                    await _localContainersClient.StopAsync(container.Id, cts.Token);
                }
                else 
                {
                    _logger.LogInformation("StopAsync: nginx is not running");
                }
            }
        }
    }
}
