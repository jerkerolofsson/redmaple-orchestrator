﻿using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.Contracts.Deployments;
using RedMaple.Orchestrator.Contracts.Healthz;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace RedMaple.Orchestrator.Controller.Infrastructure.Database
{
    internal class DeploymentPlanTemplateRepository : IDeploymentPlanTemplateRepository
    {
        private readonly ILogger _logger;
        private bool _isInitialized = false;

        public DeploymentPlanTemplateRepository(ILogger<DeploymentPlanTemplateRepository> logger)
        {
            _logger = logger;
        }

        private string GetLocalConfigDir()
        {
            string path = "/data/redmaple/controller/deployments/templates";
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (isWindows)
            {
                path = @"C:\temp\redmaple\controller\templates";
            }
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }
        public async Task AddDeploymentPlanAsync(DeploymentPlanTemplate template)
        {
            var templateDir = Path.Combine(GetLocalConfigDir(), template.Name);
            if (!Directory.Exists(templateDir))
            {
                Directory.CreateDirectory(templateDir);
            }

            var jsonPath = Path.Combine(templateDir, "template.json");
            var yamlPath = template.Kind switch
            {
                DeploymentKind.DockerCompose => Path.Combine(templateDir, "docker-compose.yaml"),
                _ => Path.Combine(templateDir, "plan.yaml")
            };

            var yaml = template.Plan;
            template.Plan = "";

            _logger.LogInformation("Adding deployment template with Name {name}..", template.Name);
            var json = JsonSerializer.Serialize(template);
            await File.WriteAllTextAsync(jsonPath, json);
            if (!string.IsNullOrWhiteSpace(yaml))
            {
                await File.WriteAllTextAsync(yamlPath, yaml);
            }

            // Restore 
            template.Plan = yaml;
        }

        public async Task<List<DeploymentPlanTemplate>> GetDeploymentPlansAsync()
        {
            if (!_isInitialized)
            {
                _isInitialized = true;
                await InitializeAsync();
            }
            var templates = await LoadDataAsync();
            return templates;
        }

        private async Task InitializeAsync() 
        {
            List<DeploymentPlanTemplate> defaultTemplates =
                [

                new DeploymentPlanTemplate
                {
                    Category = "Test",
                    Name = "Blank",
                    IconUrl = "/logo.png",
                    Plan = ""
                },

                new DeploymentPlanTemplate
                {
                    Category = "Services",
                    Name = "Rabbit MQ",
                    IconUrl = "/brands/rabbitmq.png",
                    CreateIngress = false,
                    ApplicationProtocol = "rabbitmq",
                    HealthChecks = new()
                    {
                        DeploymentHealthCheck.TcpLivez,
                    },
                    Resource = new ResourceCreationOptions
                    {
                        Create = true,
                        Exported = ["RABBITMQ_DEFAULT_USER", "RABBITMQ_DEFAULT_PASS"]
                    },
                    Plan = """
                    services:
                      rabbitmq:
                        container_name: ${REDMAPLE_DEPLOYMENT_SLUG}
                        image: "rabbitmq:3-management"
                        restart: unless-stopped
                        environment:
                          RABBITMQ_ERLANG_COOKIE: "SWQOKODSQALRPCLNMEQG"
                          RABBITMQ_DEFAULT_USER: ${RABBITMQ_DEFAULT_USER}
                          RABBITMQ_DEFAULT_PASS: ${RABBITMQ_DEFAULT_PASS}
                          RABBITMQ_DEFAULT_VHOST: "/"                        
                        ports:
                        - target: 5672
                          published: ${REDMAPLE_APP_PORT}
                        - target: 15672
                          published: ${RABBITMQ_MGMT_PORT}
                    """
                },

                new DeploymentPlanTemplate
                {
                    Category = "Services",
                    Name = "Ollama",
                    IconUrl = "/container.png",
                    CreateIngress = true,
                    ApplicationProtocol = "http",
                    HealthChecks = new()
                    {
                        new DeploymentHealthCheck()
                        {
                            Type = HealthCheckType.Livez,
                            RelativeUrl = "/",
                            HealthyStatusCode = 200,
                            Method = HealthCheckMethod.HttpGet,
                            Target = HealthCheckTarget.Application,
                        },
                        new DeploymentHealthCheck()
                        {
                            Type = HealthCheckType.Readyz,
                            RelativeUrl = "/",
                            HealthyStatusCode = 200,
                            Method = HealthCheckMethod.HttpGet,
                            Target = HealthCheckTarget.Application,
                        }
                    },

                    Plan = """
                    services:
                      libetranslate:
                        container_name: ${REDMAPLE_DEPLOYMENT_SLUG}
                        image: "ollama/ollama:latest"
                        restart: unless-stopped
                        environment:
                          LT_LOAD_ONLY: en,zh,zt,ja,ko,sv,de,fr
                        ports:
                        - target: 11434
                          published: ${REDMAPLE_APP_PORT}
                    """
                },
                new DeploymentPlanTemplate
                {
                    Category = "Services",
                    Name = "Libretranslate",
                    IconUrl = "/container.png",
                    CreateIngress = true,
                    ApplicationProtocol = "http",
                    Plan = """
                    services:
                      libetranslate:
                        container_name: ${REDMAPLE_DEPLOYMENT_SLUG}
                        image: "libretranslate/libretranslate:latest"
                        restart: unless-stopped
                        environment:
                          LT_LOAD_ONLY: en,zh,zt,ja,ko,sv,de,fr
                        ports:
                        - target: 5000
                          published: ${REDMAPLE_APP_PORT}
                    """
                },

                new DeploymentPlanTemplate
                {
                    Category = "Test",
                    Name = "Healthz Test",
                    IconUrl = "/logo.png",
                    ApplicationProtocol = "http",
                    HealthChecks = new()
                    {
                        DeploymentHealthCheck.DefaultApplicationLivez,
                        DeploymentHealthCheck.DefaultApplicationReadyz
                    },
                    Plan = """
                    services:
                      healthz:
                        container_name: ${REDMAPLE_DEPLOYMENT_SLUG}
                        image: "jerkerolofsson/redmaple-healthz-test"
                        restart: unless-stopped
                        environment:
                          ASPNETCORE_URLS: http://+:14911
                        ports:
                        - target: 14911
                          published: ${REDMAPLE_APP_PORT}
                    """
                },

                new DeploymentPlanTemplate
                {
                    Category = "Services",
                    Name = "Prometheus",
                    IconUrl = "/brands/prometheus.png",
                    ApplicationProtocol = "http",
                    HealthChecks = new()
                    {
                    },
                    Plan = """
                    services:
                      prometheus:
                        container_name: ${REDMAPLE_DEPLOYMENT_SLUG}
                        image: "prom/prometheus"
                        restart: unless-stopped
                        volumes:
                        - prom_data:/prometheus
                        ports:
                        - target: 9090
                          published: ${REDMAPLE_APP_PORT}
                    volumes:
                      prom_data:
                    """
                },


                new DeploymentPlanTemplate
                {
                    Category = "Test",
                    Name = "Blazor Sample HTTP",
                    IconUrl = "/brands/blazor.png",
                    ApplicationProtocol = "http",
                    Plan = """
                    services:
                      blazor1:
                        container_name: ${REDMAPLE_DEPLOYMENT_SLUG}
                        image: "docker-repo.dev.ec.lan:5005/ecordia-blazorapp1"
                        restart: unless-stopped
                        environment:
                          ASPNETCORE_URLS: http://+:8080
                        ports:
                        - target: 8080
                          published: ${REDMAPLE_APP_PORT}
                    """
                },
                new DeploymentPlanTemplate
                {
                    Category = "Test",
                    Name = "Blazor Sample HTTPS",
                    IconUrl = "/brands/blazor.png",
                    ApplicationProtocol = "https",
                    Plan = """
                    services:
                      blazor1:
                        container_name: ${REDMAPLE_DEPLOYMENT_SLUG}
                        image: "docker-repo.dev.ec.lan:5005/ecordia-blazorapp1"
                        restart: unless-stopped
                        environment:
                          ASPNETCORE_URLS: https://+:8081;http://+:8080
                          ASPNETCORE_Kestrel__Certificates__Default__Password: ${REDMAPLE_APP_HTTPS_CERTIFICATE_PASSWORD}
                          ASPNETCORE_Kestrel__Certificates__Default__Path: /certs/https.pfx
                        ports:
                        - target: 8081
                          published: ${REDMAPLE_APP_PORT}
                        volumes:
                          - ${REDMAPLE_APP_HTTPS_CERTIFICATE_HOST_PATH}:/certs/https.pfx:ro
                    """
                },

                new DeploymentPlanTemplate
                {
                    Category = "Test",
                    Name = "ASP.net",
                    IconUrl = "/brands/aspnet.png",
                    ApplicationProtocol = "https",
                    Plan = """
                    services:
                      portal:
                        container_name: ${REDMAPLE_DEPLOYMENT_SLUG}
                        image: ${IMAGE_NAME}
                        environment:
                          OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES: "true"
                          OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES: "true"
                          OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY: "in_memory"
                          ASPNETCORE_FORWARDEDHEADERS_ENABLED: "true"
                          ASPNETCORE_URLS: https://+:8081;http://+:8080
                          ASPNETCORE_Kestrel__Certificates__Default__Password: ${REDMAPLE_APP_HTTPS_CERTIFICATE_PASSWORD}
                          ASPNETCORE_Kestrel__Certificates__Default__Path: /certs/https.pfx
                          OIDC_IDENTITY_SERVER_URL: ${OIDC_AUTHORITY}
                          AUTH_CLIENT_ID: ${OIDC_CLIENT_ID}
                          AUTH_CLIENT_SECRET: ${OIDC_CLIENT_SECRET}
                        ports:
                        - target: 8081
                          published: ${REDMAPLE_APP_PORT}
                        restart: unless-stopped
                        volumes:
                          - ${REDMAPLE_APP_HTTPS_CERTIFICATE_HOST_PATH}:/certs/https.pfx:ro
                    """
                },

            new DeploymentPlanTemplate
            {
                Category = "Development",
                Name = "Docker Registry",
                DomainNamePrefix = "docker-repo",
                IconUrl = "/brands/docker-mark-blue.svg",
                ApplicationProtocol = "http",
                CreateIngress=false,
                Plan = """
                services:
                    docker-registry:
                        image: registry:2
                        container_name: ${REDMAPLE_DEPLOYMENT_SLUG}
                        restart: always
                        ports:
                            - ${REDMAPLE_APP_PORT}:5005
                        environment:
                            REGISTRY_HTTP_ADDR: 0.0.0.0:5005
                            REGISTRY_HTTP_HOST: ${REDMAPLE_DOMAIN_NAME}
                            REGISTRY_HTTP_SECRET: secret
                """
            },


            new DeploymentPlanTemplate
            {
                Category = "Development",
                Name = "Docker Registry Pull-through cache",
                DomainNamePrefix = "docker-cache",
                IconUrl = "/brands/docker-mark-blue.svg",
                ApplicationProtocol = "http",
                CreateIngress=false,
                Plan = """
                services:
                    docker-registry:
                        image: registry:2
                        container_name: ${REDMAPLE_DEPLOYMENT_SLUG}
                        restart: always
                        ports:
                            - ${REDMAPLE_APP_PORT}:5000
                        environment:
                            remoteurl: https://registry-1.docker.io
                            REGISTRY_HTTP_ADDR: 0.0.0.0:5000
                            REGISTRY_HTTP_HOST: ${REDMAPLE_DOMAIN_NAME}
                            REGISTRY_HTTP_SECRET: secret
                """
            },

            new DeploymentPlanTemplate
            {
                Category = "Development",
                Name = "Liget",
                IconUrl = "/brands/nuget.png",
                ApplicationProtocol = "http",
                Plan = """
                services:
                    liget:
                        image: tomzo/liget:latest
                        container_name: ${REDMAPLE_DEPLOYMENT_SLUG}
                        restart: always
                        ports:
                            - ${REDMAPLE_APP_PORT}:9011
                        environment:
                            SERVER_NAME: ${REDMAPLE_DOMAIN_NAME}
                """
            },
            new DeploymentPlanTemplate
            {
                Category = "Apps",
                Name = "mediawiki",
                IconUrl = "https://djeqr6to3dedg.cloudfront.net/repo-logos/library/mediawiki/live/logo-1720462231938.png",
                ApplicationProtocol = "http",
                Plan = """
                services:
                   mediawiki:
                     container_name: ${REDMAPLE_DEPLOYMENT_SLUG}
                     image: mediawiki
                     restart: always
                     ports:
                       - ${REDMAPLE_APP_PORT}:80
                     links:
                       - database
                     volumes:
                       - images:/var/www/html/images
                   database:
                     image: mariadb
                     restart: always
                     environment:
                       # @see https://phabricator.wikimedia.org/source/mediawiki/browse/master/includes/DefaultSettings.php
                       MYSQL_DATABASE: ${MEDIAWIKI_MYSQL_DATABASE}
                       MYSQL_USER: ${MEDIAWIKI_MYSQL_USER}
                       MYSQL_PASSWORD: ${MEDIAWIKI_MYSQL_PASSWORD}
                       MYSQL_RANDOM_ROOT_PASSWORD: 'yes'
                     volumes:
                       - db:/var/lib/mysql
                volumes:
                  images:
                  db:
                """
            },

            new DeploymentPlanTemplate
            {
                Category = "Services",
                Name = "redis",
                IconUrl = "/brands/redis.png",
                ApplicationProtocol = "redis",
                CreateIngress = false,
                HealthChecks = new()
                {
                    DeploymentHealthCheck.TcpLivez,
                },

                Plan = """
                services:
                    redis:
                        image: redis:latest
                        container_name: ${REDMAPLE_DEPLOYMENT_SLUG}
                        restart: always
                        ports:
                            - "${REDMAPLE_APP_PORT}:6379"
                        volumes:
                            - /dаta/redis:/root/redis
                            - /data/redis.conf:/usr/local/etc/redis/redis.conf
                        environment:
                            - REDIS_PASSWORD=my-password
                            - REDIS_PORT=6379
                            - REDIS_DATABASES=16
                """
            }
            ];

            foreach (var template in defaultTemplates)
            {
                await AddDeploymentPlanAsync(template);
            }
        }

        public Task DeleteDeploymentPlanAsync(string name)
        {
            _logger.LogInformation("Removing deployment template with name {name}..", name);
            var path = Path.Combine(GetLocalConfigDir(), name);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
            return Task.CompletedTask;
        }

        private async Task<List<DeploymentPlanTemplate>> LoadDataAsync()
        {
            List<DeploymentPlanTemplate> templates = new List<DeploymentPlanTemplate>();
            var path = GetLocalConfigDir();
            try
            {
                foreach (var dir in new DirectoryInfo(path).GetDirectories())
                {
                    var templatePath = Path.Combine(dir.FullName, "template.json");
                    var yamlPath = Path.Combine(dir.FullName, "plan.yaml");
                    var dockerComposePath = Path.Combine(dir.FullName, "docker-compose.yaml");
                    if (!File.Exists(templatePath))
                    {
                        continue;
                    }

                    _logger.LogInformation("Loading deployment templates from {path}", templatePath);
                    var json = await File.ReadAllTextAsync(templatePath);
                    var plan = JsonSerializer.Deserialize<DeploymentPlanTemplate>(json);
                    if(plan is not null)
                    {
                        plan.Name = dir.Name;
                        if (File.Exists(yamlPath))
                        {
                            plan.Plan = await File.ReadAllTextAsync(yamlPath);
                        }
                        if (File.Exists(dockerComposePath))
                        {
                            plan.Plan = await File.ReadAllTextAsync(dockerComposePath);
                            plan.Kind = DeploymentKind.DockerCompose;
                        }
                        templates.Add(plan);
                    }
                }
            }
            catch (Exception) { }
            return templates;
        }
    }
}
