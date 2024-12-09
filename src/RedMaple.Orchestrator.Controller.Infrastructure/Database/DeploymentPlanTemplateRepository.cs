using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.Contracts.Deployments;
using RedMaple.Orchestrator.Contracts.Healthz;
using RedMaple.Orchestrator.Contracts.Resources;

using System.Numerics;
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

        public async Task AddTemplateAsync(DeploymentPlanTemplate template)
        {
            await SaveTemplateAsync(template);
        }

        public Task DeleteTemplateAsync(string name)
        {
            var templateDir = Path.Combine(GetLocalConfigDir(), name);
            if (Directory.Exists(templateDir))
            {
                Directory.Delete(templateDir, true);
            }
            return Task.CompletedTask;
        }

            public async Task SaveTemplateAsync(DeploymentPlanTemplate template)
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

        public async Task<List<DeploymentPlanTemplate>> GetTemplatesAsync()
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
                        Kind = ResourceKind.MessageBroker,
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
                    Category = "AI",
                    Name = "Ollama",
                    IconUrl = "/apps.png",
                    CreateIngress = true,
                    ApplicationProtocol = "http",
                    DefaultDomainPrefix = "ollama",

                    Resource = new ResourceCreationOptions
                    {
                        Kind = ResourceKind.LargeLanguageModel,
                        Create = true,
                        ConnectionStringVariableNameFormat = "ConnectionStrings__${REDMAPLE_DEPLOYMENT_SLUG}",
                        ConnectionStringFormat = "Endpoint=${REDMAPLE_APP_SERVER_IP}:${REDMAPLE_APP_PORT}",
                    },
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
                      ollama:
                        container_name: ${REDMAPLE_DEPLOYMENT_SLUG}
                        image: "ollama/ollama:latest"
                        restart: unless-stopped
                        ports:
                        - target: 11434
                          published: ${REDMAPLE_APP_PORT}
                    """
                },

                new DeploymentPlanTemplate
                {
                    Category = "Monitoring",
                    Name = "Aspire",
                    IconUrl = "/brands/aspire.png",
                    CreateIngress = true,
                    ApplicationProtocol = "http",
                    DefaultDomainPrefix = "aspire",
                    UseApiPort = true,
                    Resource = new ResourceCreationOptions
                    {
                        Kind = ResourceKind.OtlpServer,
                        Create = true,
                        Exported = ["OTEL_EXPORTER_OTLP_HEADERS", "OTEL_EXPORTER_OTLP_ENDPOINT", "OTEL_EXPORTER_OTLP_PROTOCOL", "OTEL_EXPORTER_API_KEY"]
                    },
                    Plan = """
                    services:
                      aspire:
                        image: mcr.microsoft.com/dotnet/aspire-dashboard:9.0
                        container_name: ${REDMAPLE_DEPLOYMENT_SLUG}
                        restart: unless-stopped
                        environment:
                          OTEL_EXPORTER_OTLP_PROTOCOL: grpc
                          OTEL_EXPORTER_OTLP_ENDPOINT: http://${REDMAPLE_APP_SERVER_IP}:${REDMAPLE_API_PORT}
                          OTEL_EXPORTER_OTLP_HEADERS: x-otlp-api-key=${OTEL_EXPORTER_API_KEY}
                          OTEL_EXPORTER_API_KEY: ${OTEL_EXPORTER_API_KEY}
                          DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS: true
                          DASHBOARD__TELEMETRYLIMITS__MAXLOGCOUNT='1000'
                          DASHBOARD__TELEMETRYLIMITS__MAXTRACECOUNT='1000'
                          DASHBOARD__TELEMETRYLIMITS__MAXMETRICSCOUNT='1000'
                          DOTNET_DASHBOARD_OTLP_ENDPOINT_URL: http://0.0.0.0:18889
                        ports:
                        - target: 18888
                          published: ${REDMAPLE_APP_PORT}
                        - target: 18889
                          published: ${REDMAPLE_API_PORT}
                    """
                },

                new DeploymentPlanTemplate
                {
                    Category = "AI",
                    Name = "Ollama GPU",
                    IconUrl = "/apps.png",
                    CreateIngress = true,
                    ApplicationProtocol = "http",
                    DefaultDomainPrefix = "ollama-gpu",

                    Resource = new ResourceCreationOptions
                    {
                        Kind = ResourceKind.LargeLanguageModel,
                        Create = true,
                        ConnectionStringVariableNameFormat = "ConnectionStrings__${REDMAPLE_DEPLOYMENT_SLUG}",
                        ConnectionStringFormat = "Endpoint=${REDMAPLE_APP_SERVER_IP}:${REDMAPLE_APP_PORT}",
                    },
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
                      ollama_gpu:
                        container_name: ${REDMAPLE_DEPLOYMENT_SLUG}
                        image: "ollama/ollama:latest"
                        restart: unless-stopped
                        ports:
                        - target: 11434
                          published: ${REDMAPLE_APP_PORT}
                        deploy:
                          resources:
                            reservations:
                              devices:
                                - driver: nvidia
                                  count: 1
                                  capabilities: [gpu]
                    """
                },
                new DeploymentPlanTemplate
                {
                    Category = "Services",
                    Name = "Libretranslate",
                    IconUrl = "/apps.png",
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
                    Category = "Monitoring",
                    Name = "Loki",
                    Description = "Loki is a log aggregation system designed to store and query logs from all your applications and infrastructure",
                    IconUrl = "/brands/grafana.png",
                    ApplicationProtocol = "http",
                    HealthChecks = new()
                    {
                    },
                    Plan = """
                    services:
                      grafana:
                        container_name: ${REDMAPLE_DEPLOYMENT_SLUG}
                        image: "grafana/loki:latest"
                        restart: unless-stopped
                        volumes:
                        - ./grafana:/etc/grafana/provisioning/datasources
                        ports:
                        - target: 3100
                          published: ${REDMAPLE_APP_PORT}
                    """
                },

                new DeploymentPlanTemplate
                {
                    Category = "Monitoring",
                    Name = "Grafana",
                    IconUrl = "/brands/grafana.png",
                    ApplicationProtocol = "http",
                    HealthChecks = new()
                    {
                    },
                    Plan = """
                    services:
                      grafana:
                        container_name: ${REDMAPLE_DEPLOYMENT_SLUG}
                        image: "grafana/grafana-oss:latest"
                        restart: unless-stopped
                        volumes:
                        - ./grafana:/etc/grafana/provisioning/datasources
                        ports:
                        - target: 3000
                          published: ${REDMAPLE_APP_PORT}
                        environment:
                        - GF_PATHS_PROVISIONING=/etc/grafana/provisioning
                        - GF_AUTH_ANONYMOUS_ENABLED=true
                        - GF_AUTH_ANONYMOUS_ORG_ROLE=Admin
                        - GF_FEATURE_TOGGLES_ENABLE=alertingSimplifiedRouting,alertingQueryAndExpressionsStepMode
                    volumes:
                      grafana:
                    """
                },

                new DeploymentPlanTemplate
                {
                    Category = "Monitoring",
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
                     // mongodb://admin:password@localhost:15433
                    Resource = new ResourceCreationOptions
                    {
                        Create = true,
                        Kind = ResourceKind.ConnectionString,
                        ConnectionStringVariableNameFormat = "ConnectionStrings__${REDMAPLE_DEPLOYMENT_SLUG}",
                        ConnectionStringFormat = "mongodb://${MONGODB_USERNAME}:${MONGODB_PASSWORD}@${REDMAPLE_APP_SERVER_IP}:${REDMAPLE_APP_PORT}",
                        Exported = ["MONGODB_USERNAME", "MONGODB_PASSWORD"]
                    },
                    Category = "Databases",
                    CreateIngress= false,
                    Name = "MongoDB 6.0",
                    IconUrl = "/brands/mongodb.png",
                    ApplicationProtocol = "tcp",
                    Plan = """
                    services:
                      mongodb:
                        container_name: ${REDMAPLE_DEPLOYMENT_SLUG}
                        image: "mongo:6.0"
                        restart: unless-stopped
                        environment:
                          MONGO_INITDB_ROOT_USERNAME: ${MONGODB_USERNAME}
                          MONGO_INITDB_ROOT_PASSWORD: ${MONGODB_PASSWORD}
                        volumes:
                        - "MONGODATA:/data/db"
                        ports:
                        - target: 27017
                          published: ${REDMAPLE_APP_PORT}
                    """
                },

            new DeploymentPlanTemplate
            {
                 Resource = new ResourceCreationOptions
                {
                    Create = true,
                    Kind = ResourceKind.ConnectionString,
                    ConnectionStringVariableNameFormat = "ConnectionStrings__${REDMAPLE_DEPLOYMENT_SLUG}",
                    ConnectionStringFormat = "${REDMAPLE_APP_SERVER_IP}:${REDMAPLE_APP_PORT},password=${REDIS_PASSWORD}",
                    Exported = ["REDIS_PASSWORD"]
                },
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
                            - REDIS_PASSWORD=${REDIS_PASSWORD}
                            - REDIS_PORT=6379
                            - REDIS_DATABASES=16
                """
            },
                new DeploymentPlanTemplate
                {
                    Resource = new ResourceCreationOptions
                    {
                        // User ID=user;Password=password;Host=localhost;Port=15432;Database=redmaple;Pooling=true;Integrated Security=false;Maximum Pool Size=1024;
                        Create = true,
                        Kind = ResourceKind.ConnectionString,
                        ConnectionStringVariableNameFormat = "ConnectionStrings__${POSTGRES_DB}",
                        ConnectionStringFormat = "User ID=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}Host=${REDMAPLE_APP_SERVER_IP};Port=${REDMAPLE_APP_PORT};Database=${POSTGRES_DB};Pooling=true;Integrated Security=false;Maximum Pool Size=1024;",
                        Exported = ["POSTGRES_DB", "POSTGRES_USER", "POSTGRES_PASSWORD"]
                    },
                    Category = "Databases",
                    CreateIngress= false,
                    Name = "PostgreSQL",
                    IconUrl = "/brands/postgres.png",
                    ApplicationProtocol = "tcp",
                    Plan = """
                    services:
                      postgres:
                        container_name: ${REDMAPLE_DEPLOYMENT_SLUG}
                        image: "postgres:15.3"
                        restart: unless-stopped
                        environment:
                          POSTGRES_DB: ${POSTGRES_DB}
                          POSTGRES_USER: ${POSTGRES_USER}
                          POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
                        volumes:
                        - "PGDATA:/var/lib/postgresql/data"
                        ports:
                        - target: 5432
                          published: ${REDMAPLE_APP_PORT}
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
                IconUrl = "/brands/mediawiki.png",
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
                Name = "Keycloak",
                IconUrl = "/brands/keycloak.png",
                ApplicationProtocol = "http",

                HealthChecks = new()
                {
                    new DeploymentHealthCheck()
                    {
                        Type = HealthCheckType.Readyz,
                        RelativeUrl = "/health/ready",
                        HealthyStatusCode = 200,
                        Method = HealthCheckMethod.HttpGet,
                        Target = HealthCheckTarget.Application,
                    },
                    new DeploymentHealthCheck()
                    {
                        Type = HealthCheckType.Livez,
                        RelativeUrl = "/health/live",
                        HealthyStatusCode = 200,
                        Method = HealthCheckMethod.HttpGet,
                        Target = HealthCheckTarget.Application,
                    },
                },

                Resource = new ResourceCreationOptions
                {
                    Create = true,
                    Kind = ResourceKind.OidcServer,
                    Exported = ["OIDC_AUTHORITY"]
                },

                Plan = """
                name: 
                services:
                  keycloak_web:
                    image: quay.io/keycloak/keycloak:26.0.6
                    container_name: keycloak_web
                    hostname: 
                    domainname: 
                    restart: 
                    ports:
                    - published: ${REDMAPLE_APP_PORT}
                      target: 8080
                    deploy: 
                    dns:
                    - 192.168.0.202
                    dns_opt: 
                    dns_search: 
                    depends_on:
                    - keycloakdb
                    command: start-dev
                    cap_add: 
                    networks: 
                    volumes: []
                    environment:
                      OIDC_AUTHORITY: "https://${REDMAPLE_DOMAIN_NAME}/realms/master"
                      KC_DB: "postgres"
                      KC_DB_URL: "jdbc:postgresql://keycloakdb:5432/keycloak"
                      KC_DB_USERNAME: "keycloak"
                      KC_DB_PASSWORD: "password"
                      KC_HOSTNAME: "https://${REDMAPLE_DOMAIN_NAME}"
                      KC_HOSTNAME_PORT: "443"
                      KC_HOSTNAME_STRICT: "true"
                      KC_HOSTNAME_STRICT_HTTPS: "true"
                      KC_HTTP_ENABLED: "false"
                      KC_LOG_LEVEL: "info"
                      KC_METRICS_ENABLED: "true"
                      KC_HEALTH_ENABLED: "true"
                      KC_BOOTSTRAP_ADMIN_USERNAME: "admin"
                      KC_BOOTSTRAP_ADMIN_PASSWORD: "admin"
                      KC_PROXY_HEADERS: "xforwarded"

                  keycloakdb:
                    image: postgres:15
                    container_name: 
                    hostname: 
                    domainname: 
                    restart: 
                    ports: []
                    deploy: 
                    dns: 
                    dns_opt: 
                    dns_search: 
                    depends_on: 
                    command: 
                    cap_add: 
                    networks: 
                    volumes:
                    - source: /data/keycloak/postgres
                      target: /var/lib/postgresql/data
                    environment:
                      POSTGRES_DB: "keycloak"
                      POSTGRES_USER: "keycloak"
                      POSTGRES_PASSWORD: "password"
                networks: {}
                volumes: {}
                
                """
            },

            new DeploymentPlanTemplate
            {
                Resource = new ResourceCreationOptions
                {
                    Create = false
                },
                Category = "Apps",
                Name = "wikijs",
                IconUrl = "/brands/wikijs.png",
                ApplicationProtocol = "http",
                Plan = """
                services:
                  wikijs:
                    image: lscr.io/linuxserver/wikijs:latest
                    container_name: ${REDMAPLE_DEPLOYMENT_SLUG}
                    environment:
                      - PUID=1000
                      - PGID=1000
                      - TZ=Etc/UTC
                      - DB_TYPE=sqlite #optional
                    volumes:
                      - /data/wikijs/config:/config
                      - /data/wikijs/data:/data
                    ports:
                      - ${REDMAPLE_APP_PORT}:3000
                    restart: unless-stopped
                """
            },


            new DeploymentPlanTemplate
            {
                Category = "Networking",
                Name = "unifi",
                IconUrl = "/brands/unifi.png",
                ApplicationProtocol = "https",
                HealthChecks = new()
                {
                    DeploymentHealthCheck.TcpLivez,
                },

                Plan = """
                services:
                    redis:
                        image: lscr.io/linuxserver/unifi-controller:latest
                        container_name: ${REDMAPLE_DEPLOYMENT_SLUG}
                        restart: always
                        ports:
                        - "${REDMAPLE_APP_PORT}:8443"
                        - 3478:3478/udp
                        - 10001:10001/udp
                        - 8080:8080
                        - 1900:1900/udp
                        - 8843:8843
                        - 8880:8880
                        - 6789:6789
                        - 5514:5514/udp
                        volumes:
                         - /data/unifi:/config
                        environment:
                          - PUID=1000
                          - PGID=1000
                          - TZ=Etc/UTC
                          - MEM_LIMIT=1024
                          - MEM_STARTUP=1024
                """
            },


            new DeploymentPlanTemplate
            {
                Category = "Apps",
                Name = "Krita",
                IconUrl = "/brands/krita.png",
                ApplicationProtocol = "http",
                HealthChecks = new()
                {
                    DeploymentHealthCheck.TcpLivez,
                },

                Plan = """
                services:
                    redis:
                        image: lscr.io/linuxserver/krita:latest
                        container_name: ${REDMAPLE_DEPLOYMENT_SLUG}
                        restart: unless-stopped
                        ports:
                        - "${REDMAPLE_APP_PORT}:3000"
                        environment:
                        - PUID=1000
                        - PGID=1000
                        - TZ=Etc/UTC                        
                
                """
            },
            ];

            foreach (var template in defaultTemplates)
            {
                await AddTemplateAsync(template);
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
                    DeploymentPlanTemplate? template = await LoadTemplateFromDirAsync(dir);
                    if (template is not null)
                    {
                        templates.Add(template);
                    }
                }
            }
            catch (Exception) { }
            return templates;
        }

        private async Task<DeploymentPlanTemplate?> LoadTemplateFromDirAsync(DirectoryInfo dir)
        {
            if(!dir.Exists)
            {
                return null;
            }

            var templatePath = Path.Combine(dir.FullName, "template.json");
            var yamlPath = Path.Combine(dir.FullName, "plan.yaml");
            var dockerComposePath = Path.Combine(dir.FullName, "docker-compose.yaml");
            if (!File.Exists(templatePath))
            {
                return null;
            }

            _logger.LogTrace("Loading deployment templates from {path}", templatePath);
            var json = await File.ReadAllTextAsync(templatePath);
            var plan = JsonSerializer.Deserialize<DeploymentPlanTemplate>(json);
            if (plan is not null)
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
            }

            return plan;
        }

        public async Task<DeploymentPlanTemplate?> GetTemplateByNameAsync(string name)
        {
            var path = Path.Combine(GetLocalConfigDir(), name);
            return await LoadTemplateFromDirAsync(new DirectoryInfo(path));
        }
    }
}
