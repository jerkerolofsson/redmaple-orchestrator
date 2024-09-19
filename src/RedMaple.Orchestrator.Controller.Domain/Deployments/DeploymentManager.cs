using Docker.DotNet.Models;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.Contracts.Containers;
using RedMaple.Orchestrator.Contracts.Deployments;
using RedMaple.Orchestrator.Contracts.Ingress;
using RedMaple.Orchestrator.Contracts.Node;
using RedMaple.Orchestrator.Controller.Domain.Domain;
using RedMaple.Orchestrator.Controller.Domain.GlobalDns;
using RedMaple.Orchestrator.Controller.Domain.Healthz;
using RedMaple.Orchestrator.Controller.Domain.Ingress;
using RedMaple.Orchestrator.Controller.Domain.Node;
using RedMaple.Orchestrator.DockerCompose;
using RedMaple.Orchestrator.Sdk;
using RedMaple.Orchestrator.Security.Builder;
using RedMaple.Orchestrator.Security.Serialization;
using RedMaple.Orchestrator.Security.Services;
using RedMaple.Orchestrator.Utilities;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;

namespace RedMaple.Orchestrator.Controller.Domain.Deployments
{
    internal class DeploymentManager : IDeploymentManager
    {
        private readonly IHealthzChecker _healthChecker;
        private readonly IGlobalDns _dns;
        private readonly IMediator _mediator;
        private readonly IDomainService _domain;
        private readonly string _secretCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890";
        private readonly IIngressManager _ingressManager;
        private readonly INodeManager _nodeManager;
        private readonly ICertificateAuthority _ca;
        private readonly IDeploymentPlanRepository _deploymentPlanRepository;
        private readonly IApplicationDeploymentRepository _appDeploymentRepository;
        private readonly ILogger<DeploymentManager> _logger;

        public DeploymentManager(
            IDomainService domain,
            IIngressManager ingressManager,
            INodeManager nodeManager,
            ICertificateAuthority certificateAuthority,
            IApplicationDeploymentRepository appDeploymentRepository,
            IDeploymentPlanRepository deploymentPlanRepository,
            IGlobalDns dns,
            IMediator mediator,
            ILogger<DeploymentManager> logger,
            IHealthzChecker healthChecker)
        {
            _appDeploymentRepository = appDeploymentRepository;
            _domain = domain;
            _ingressManager = ingressManager;
            _nodeManager = nodeManager;
            _ca = certificateAuthority;
            _deploymentPlanRepository = deploymentPlanRepository;
            _dns = dns;
            _mediator = mediator;
            _logger = logger;
            _healthChecker = healthChecker;
        }

        private async Task<NodeInfo?> GetApplicationHostAsync(string applicationServerIp)
        {
            return await _nodeManager.GetNodeByIpAddressAsync(applicationServerIp);
        }
        private async Task<NodeInfo?> GetApplicationHostAsync(Deployment deployment)
        {
            if (deployment?.ApplicationServerIp is null)
            {
                return null;
            }
            return await _nodeManager.GetNodeByIpAddressAsync(deployment.ApplicationServerIp);
        }

        public async Task<List<DeployedContainer>> GetDeployedContainersAsync(string slug)
        {
            var deployment = await GetDeploymentBySlugAsync(slug);
            return await GetDeployedContainersAsync(deployment);
        }

        /// <summary>
        /// Returns actual application deployments
        /// </summary>
        /// <param name="slug"></param>
        /// <returns></returns>
        public async Task<List<Deployment>> GetApplicationDeploymentsAsync(string slug)
        {
            var deployments = await _appDeploymentRepository.GetDeploymentsAsync();
            return deployments.Where(x => x.Slug == slug).ToList();
        }

        public string CreateSlug(string deploymentName)
        {
            return SlugGenerator.Generate(deploymentName);
        }

        public async Task<List<DeployedContainer>> GetDeployedContainersAsync(Deployment appDeployment)
        {
            var containers = new List<DeployedContainer>();

            var node = await GetApplicationHostAsync(appDeployment);
            if (node is null)
            {
                return containers;
            }
            using var client = new NodeContainersClient(node.BaseUrl);
            var nodeContainers = await client.GetContainersForDeploymentAsync(appDeployment.Slug);
            foreach (var container in nodeContainers)
            {
                containers.Add(
                    new DeployedContainer(
                        node,
                        appDeployment.ApplicationServerIp,
                        appDeployment.ApplicationServerPort,
                        container));
            }
            return containers;
        }
        
        public async Task<List<DeployedContainer>> GetDeployedContainersAsync(DeploymentPlan? deployment)
        {
            var containers = new List<DeployedContainer>();
            if (deployment?.Slug is not null)
            {
                foreach (var appDeployment in await GetApplicationDeploymentsAsync(deployment.Slug))
                {
                    containers.AddRange(await GetDeployedContainersAsync(appDeployment));
                }
            }
            return containers;
        }

        public async Task<DeploymentPlan?> GetDeploymentBySlugAsync(string slug)
        {
            var plans = await GetDeploymentPlansAsync();
            return plans.Where(x => x.Slug == slug).FirstOrDefault();
        }

        public async Task<ValidationResult> ValidatePlanAsync(DeploymentPlan plan)
        {
            var plans = await GetDeploymentPlansAsync();
            var planNames = plans.Select(x => x.Name).ToHashSet();
            var planSlugs = plans.Select(x => x.Slug).ToHashSet();
            var dnsEntries = (await _dns.GetDnsEntriesAsync()).Select(x => x.Hostname).ToHashSet();
            var result = new ValidationResult();

            if (plan.CreateDnsEntry || plan.CreateIngress)
            {
                if (string.IsNullOrWhiteSpace(plan.DomainName))
                {
                    result.Errors.Add(new ValidationFailure("DomainName", $"Domain name is missing"));
                }
                else if (dnsEntries.Contains(plan.DomainName))
                {
                    result.Errors.Add(new ValidationFailure("DomainName", $"The domain name is already in use"));
                }
            }
            if (planNames.Contains(plan.Name))
            {
                result.Errors.Add(new ValidationFailure("Name", $"The name is already in use"));
            }
            if (planSlugs.Contains(plan.Slug))
            {
                result.Errors.Add(new ValidationFailure("Name", $"The name (slug) is already in use"));
            }

            foreach (var env in plan.EnvironmentVariables)
            {
                if(string.IsNullOrEmpty(env.Value))
                {
                    result.Errors.Add(new ValidationFailure("EnvironmentVariables", $"The environment variable '{env.Key}' is missing"));
                }
            }

            plan.ContainerImages.Clear();
            if (plan.Kind == DeploymentKind.DockerCompose)
            {
                try
                {
                    var composePlan = DockerComposeParser.ParseYaml(plan.Plan);
                    if (composePlan?.services is not null)
                    {
                        foreach (var service in composePlan.services.Values)
                        {
                            if (service?.image is not null)
                            {
                                plan.ContainerImages.Add(service.image);
                            }
                        }
                    }
                }
                catch (Exception) 
                {
                    result.Errors.Add(new ValidationFailure("Plan", "Failed to parse docker compose"));
                }
            }

            if (string.IsNullOrEmpty(plan.Name))
            {
                result.Errors.Add(new ValidationFailure("Name", "Name is mandatory"));
            }
            if (plan.CreateDnsEntry)
            {
                if (string.IsNullOrEmpty(plan.DomainName))
                {
                    result.Errors.Add(new ValidationFailure("DomainName", "DomainName is required when CreateDnsEntry is true"));
                }
            }
            if (plan.CreateIngress)
            {
                if (string.IsNullOrEmpty(plan.IngressServerIp))
                {
                    result.Errors.Add(new ValidationFailure("IngressServerIp", "IngressServerIp is required when CreateIngress is true"));
                }
            }
            if (plan.ApplicationServerIps.Count == 0)
            {
                result.Errors.Add(new ValidationFailure("ApplicationServerIp", "ApplicationServerIp is mandatory"));
            }
            if (plan.EnvironmentVariables is not null &&
                plan.EnvironmentVariables.TryGetValue("REDMAPLE_APP_PORT", out string? appPort) &&
                int.TryParse(appPort, out var port))
            {
                plan.ApplicationServerPort = port;
            }
            if (plan.ApplicationServerPort is null)
            {
                result.Errors.Add(new ValidationFailure("ApplicationServerPort", "ApplicationServerPort is missing (REDMAPLE_APP_PORT environment variable)"));
            }

            if (plan.EnvironmentVariables is not null &&
                plan.EnvironmentVariables.TryGetValue("REDMAPLE_APP_HTTPS_CERTIFICATE_PASSWORD", out string? httpsCertificatePassword))
            {
                plan.ApplicationHttpsCertificatePassword = httpsCertificatePassword;
            }
            if (plan.ApplicationHttpsCertificatePassword is null)
            {
                result.Errors.Add(new ValidationFailure("ApplicationHttpsCertificatePassword", "ApplicationHttpsCertificatePassword is missing (REDMAPLE_APP_HTTPS_CERTIFICATE_PASSWORD environment variable)"));
            }
            

            return result;
        }

        public Dictionary<string, string> GetDefaultEnvironmentVariables(DeploymentPlan plan)
        {
            var environmentVariables = new Dictionary<string, string>();
            environmentVariables["REDMAPLE_DEFAULT_DOMAIN"] = _domain.DefaultDomain;
            environmentVariables["REDMAPLE_DEPLOYMENT_NAME"] = plan.Name;
            environmentVariables["REDMAPLE_DEPLOYMENT_SLUG"] = plan.Slug;
            environmentVariables["REDMAPLE_APP_PORT"] = plan.ApplicationServerPort?.ToString() ?? "";

            environmentVariables["REDMAPLE_APP_HTTPS_CERTIFICATE_HOST_PATH"] = $"/data/redmaple/node/certificates/{plan.Slug}/https.pfx";
            environmentVariables["REDMAPLE_APP_HTTPS_CERTIFICATE_PASSWORD"] = RandomNumberGenerator.GetString(_secretCharacters, 32);

            // Forward some system environment variables
            if (!string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("OIDC_AUTHORITY")))
            {
                environmentVariables["OIDC_AUTHORITY"] = System.Environment.GetEnvironmentVariable("OIDC_AUTHORITY") ?? "";
            }

            if (plan.DomainName is not null)
            {
                environmentVariables["REDMAPLE_DOMAIN_NAME"] = plan.DomainName;
            }

            if(plan.Kind == DeploymentKind.DockerCompose)
            {
                try
                {
                    foreach(var name in DockerComposeParser.FindEnvironmentVariables(plan.Plan))
                    {
                        if(!environmentVariables.ContainsKey(name))
                        {
                            environmentVariables[name] = Environment.GetEnvironmentVariable(name) ?? "";
                        }
                    }
                }
                catch (Exception) { }
            }


            return environmentVariables;
        }

        public async Task SaveAsync(DeploymentPlan plan)
        {
            plan.Id = plan.Slug;
            await _deploymentPlanRepository.AddDeploymentPlanAsync(plan);
        }

        public async Task DeleteAsync(DeploymentPlan plan, IProgress<string> progress, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting deployments for {DeploymentSlug}", plan.Slug);
            foreach (var deployment in await GetApplicationDeploymentsAsync(plan.Slug))
            {
                var targetNode = await _nodeManager.GetNodeByIpAddressAsync(deployment.ApplicationServerIp);
                if (targetNode is null)
                {
                    _logger.LogError("Cannot delete deployment {DeploymentSlug} as application node was not found", plan.Slug);
                    throw new ArgumentException("Application node not found: " + deployment.ApplicationServerIp);
                }

                _logger.LogInformation("Deleting deployment at {IpAddress} for {DeploymentSlug}", targetNode.IpAddress, plan.Slug);
                await DeleteDeploymentFromTargetAsync(plan, progress, targetNode, cancellationToken);
           }

            _logger.LogInformation("Deleting deployment plan for {DeploymentSlug}", plan.Slug);
            await _deploymentPlanRepository.DeleteDeploymentPlanAsync(plan.Id);
        }

        public async Task PullImagesAsync(
            DeploymentPlan plan,
            IProgress<string> progress, 
            IProgress<JSONMessage> pullProgress, CancellationToken cancellationToken)
        {
            foreach(var applicationServerIp in plan.ApplicationServerIps)
            {
                var targetNode = await GetApplicationHostAsync(applicationServerIp);
                if (targetNode is null)
                {
                    continue;
                }

                foreach(var containerImage in plan.ContainerImages)
                {
                    progress.Report($"Pulling {containerImage} on {applicationServerIp}..");

                    using var client = new NodeContainersClient(targetNode.BaseUrl);
                    await client.PullImageAsync(containerImage, pullProgress, cancellationToken);
                }
            }
        }

        public async Task TakeDownAsync(DeploymentPlan plan, IProgress<string> progress, CancellationToken cancellationToken)
        {
            if (plan.CreateIngress)
            {
                await _ingressManager.TryDeleteIngressServiceByDomainNameAsync(plan.DomainName);
            }
            else if(plan.CreateDnsEntry)
            {
                await _dns.TryDeleteDnsEntryByDomainNameAsync(plan.DomainName);
            }

            foreach (var deployment in await GetApplicationDeploymentsAsync(plan.Slug))
            {
                var targetNode = await GetApplicationHostAsync(deployment);
                if (targetNode is null)
                {
                    throw new ArgumentException("Application node not found: " + deployment.ApplicationServerIp);
                }

                await _mediator.Publish(new AppDeploymentStoppingNotification(deployment));

                try
                {
                    await DownDeploymentOnTargetAsync(plan, progress, targetNode, cancellationToken);
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "Failed to bring down deployment {DeploymentSlug} on node: {Node}", deployment.Slug, targetNode.IpAddress);
                    throw;
                }

                await _appDeploymentRepository.DeleteDeploymentAsync(deployment.Id);
            }

            // Save state
            plan.Up = false;
            await _deploymentPlanRepository.SaveDeploymentPlanAsync(plan);
        }

        public async Task BringUpAsync(DeploymentPlan plan, IProgress<string> progress, CancellationToken cancellationToken)
        {
            await ValidatePlanAsync(plan);
            plan.Health = new Healthz.Models.ResourceHealthCheckResult { Status = HealthStatus.Degraded };

            // Todo: find nodes

            var applicationServerIp = plan.ApplicationServerIps.First();
            ArgumentNullException.ThrowIfNull(plan.ApplicationHttpsCertificatePassword);
            var targetNode = await _nodeManager.GetNodeByIpAddressAsync(applicationServerIp);
            if (targetNode is null)
            {
                throw new ArgumentException("Application node not found: " + applicationServerIp);
            }

            if (plan.CreateDnsEntry)
            {
                await CreateDnsAndIngressAsync(plan, progress);
            }
            else if (plan.CreateIngress)
            {
                throw new InvalidDataException("CreateDnsEntry is false but CreateIngress is true. This combination is not valid.");
            }

            // Record of deployment which is kept locally on controller
            var deploymentId = plan.Slug + "-" + Guid.NewGuid().ToString();
            var appDeployment = new Deployment(
                deploymentId,
                plan.Slug,
                plan.Resource,
                applicationServerIp,
                plan.ApplicationServerPort,
                plan.ApplicationProtocol
            );

            // Generate pfx certificate for the application server
            if (plan.ApplicationHttpsCertificatePfx is null || plan.ApplicationHttpsCertificatePfx.Length == 0)
            {
                await GenerateHttpsCertificateAsync(plan, progress, cancellationToken);
                await _deploymentPlanRepository.SaveDeploymentPlanAsync(plan);
            }

            await AddDeploymentOnTargetAsync(plan, progress, targetNode);
            await _mediator.Publish(new AppDeploymentStartingNotification(appDeployment));
            await UpDeploymentOnTargetAsync(plan, progress, targetNode, cancellationToken);
            await _appDeploymentRepository.SaveDeploymentAsync(appDeployment);

            await WaitUntilReadyzAsync(plan, appDeployment, progress, cancellationToken);
            await _mediator.Publish(new AppDeploymentReadyNotification(appDeployment));

            // Save state
            plan.Up = true;
            plan.Health = new() { Status = HealthStatus.Healthy };
            await _deploymentPlanRepository.SaveDeploymentPlanAsync(plan);
        }

        public async Task WaitUntilReadyzAsync(
            DeploymentPlan plan,
            Deployment applicationDeployment,
            IProgress<string> progress, CancellationToken cancellationToken)
        {
            var readyz = plan.HealthChecks.Where(x => x.Type == Contracts.Healthz.HealthCheckType.Readyz).ToList();
            if (readyz.Count == 0)
            {
                progress.Report("No readyz health checks");
                return;
            }

            TimeSpan timeout = TimeSpan.FromSeconds(30);
            var startTimestamp = Stopwatch.GetTimestamp();
            while(timeout > Stopwatch.GetElapsedTime(startTimestamp))
            {
                foreach (var check in readyz)
                {
                    var status = await _healthChecker.CheckLivezAsync(applicationDeployment, plan, cancellationToken);
                    progress.Report($"readyz check => {status}");
                    if (status == HealthStatus.Healthy)
                    {
                        return;
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }

            progress.Report("Failed when waiting for readyz");
            throw new Exception("Failed to verify that deployment is up (readyz health check)");
        }

        private static async Task DeleteDeploymentFromTargetAsync(DeploymentPlan plan, IProgress<string> progress, NodeInfo targetNode, CancellationToken cancellationToken)
        {
            progress.Report($"Deleting deployment from {targetNode.IpAddress}..");
            var deploymentClient = new NodeDeploymentClient(targetNode.BaseUrl);
            await deploymentClient.DeleteAsync(plan.Slug, progress);
        }
        private static async Task UpDeploymentOnTargetAsync(DeploymentPlan plan, IProgress<string> progress, NodeInfo targetNode, CancellationToken cancellationToken)
        {
            progress.Report($"Starting deployment on {targetNode.IpAddress}..");
            var deploymentClient = new NodeDeploymentClient(targetNode.BaseUrl);
            await deploymentClient.UpAsync(plan.Slug, progress);
        }

        private static async Task DownDeploymentOnTargetAsync(DeploymentPlan plan, IProgress<string> progress, NodeInfo targetNode, CancellationToken cancellationToken)
        {
            plan.Health = new() { Status = HealthStatus.Unhealthy };

            progress.Report($"Stopping deployment on {targetNode.IpAddress}..");
            var deploymentClient = new NodeDeploymentClient(targetNode.BaseUrl);
            await deploymentClient.DownAsync(plan.Slug, progress);
        }

        private static async Task AddDeploymentOnTargetAsync(DeploymentPlan plan, IProgress<string> progress, NodeInfo targetNode)
        {
            ArgumentNullException.ThrowIfNull(plan.ApplicationHttpsCertificatePfx);

            progress.Report($"Adding deployment on {targetNode.IpAddress}..");
            var deploymentClient = new NodeDeploymentClient(targetNode.BaseUrl);
            await deploymentClient.AddDeploymentAsync(new AddNodeDeploymentRequest
            {
                EnvironmentVariables = plan.EnvironmentVariables,
                HttpsCertificatePfx = plan.ApplicationHttpsCertificatePfx,
                Kind = plan.Kind,
                Plan = plan.Plan,
                Slug = plan.Slug
            }, progress);
        }

        private async Task GenerateHttpsCertificateAsync(DeploymentPlan plan, IProgress<string> progress, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(plan.ApplicationHttpsCertificatePassword);

            progress.Report("Generating HTTPS application certificate..");

            var root = await _ca.GetRootCertificateAsync();
            var issuer = await _ca.GetCaCertificateAsync();

            var name = plan.DomainName + ".app";

            var builder = new CertificateBuilder()
                .AddCommonName(name)
                .WithExpiry(TimeSpan.FromDays(365 * 3))
                .WithOid(CertificateBuilder.OID_TLS_WEB_SERVER_AUTHENTICATION)
                .WithSubjectAlternativeName((sanBuilder) =>
                {
                    foreach(var ip in plan.ApplicationServerIps)
                    { 
                        sanBuilder.AddIpAddress(IPAddress.Parse(ip));
                    }
                });

            var password = plan.ApplicationHttpsCertificatePassword;
            builder.SetPassword(password);

            var cert = builder.BuildSignedCertificate(issuer);
            var pfxBytes = CertificateSerializer.ToPfx(cert, password, [issuer, root]);
            plan.ApplicationHttpsCertificatePfx = pfxBytes;
        }

        private async Task CreateDnsAndIngressAsync(DeploymentPlan plan, IProgress<string> progress)
        {
            if (string.IsNullOrEmpty(plan.DomainName))
            {
                throw new ArgumentException(nameof(plan.DomainName));
            }

            // Todo: LB: required if multiple ApplicationServerIps are set
            // 

            // We have two scenarios here.
            // If we have an ingress node then we set the DNS to it
            // If not, we set the DNS directly to the application IP

            var applicationServerIp = plan.ApplicationServerIps.First();

            if (!plan.CreateIngress)
            {
                var entries = await _dns.GetDnsEntriesAsync();
                var existingEntry = entries.FirstOrDefault(x => x.Hostname == plan.DomainName);
                if(existingEntry is null)
                {
                    progress.Report($"Creating DNS entry {plan.Slug} for {plan.DomainName} to {applicationServerIp}..");
                    _logger.LogInformation("Deployment plan DNS entry created for {domainName}={new}", plan.DomainName, applicationServerIp);
                    entries.Add(new Contracts.Dns.DnsEntry { IsGlobal = true, Hostname = plan.DomainName, IpAddress = applicationServerIp });
                    await _dns.SetDnsEntriesAsync(entries);
                }
                else if(existingEntry.IpAddress != applicationServerIp)
                {
                    progress.Report($"Updating DNS for {plan.Slug} from {existingEntry.IpAddress} to {applicationServerIp}..");

                    _logger.LogWarning("Deployment plan DNS entry changed from {old} to {new}", existingEntry.IpAddress, applicationServerIp);
                    existingEntry.IpAddress = applicationServerIp;
                    await _dns.SetDnsEntriesAsync(entries);
                }
                else
                {
                    _logger.LogDebug("DNS entry already exists");
                }
            }
            else
            {
                if (string.IsNullOrEmpty(plan.IngressServerIp))
                {
                    throw new ArgumentException(nameof(plan.IngressServerIp));
                }
                if (plan.ApplicationServerPort is null)
                {
                    throw new ArgumentException(nameof(plan.ApplicationServerPort));
                }
                await _ingressManager.AddIngressServiceAsync(new IngressServiceDescription
                {
                    IngressIp = plan.IngressServerIp,
                    DomainName = plan.DomainName,
                    IngressPort = 443,
                    Scheme = plan.ApplicationProtocol,
                    Id = plan.Slug + "-ingress",
                    DestinationIp = applicationServerIp,
                    DestinationPort = plan.ApplicationServerPort.Value
                }, progress);
            }
        }

        public async Task<List<DeploymentPlan>> GetDeploymentPlansAsync()
        {
            return await _deploymentPlanRepository.GetDeploymentPlansAsync();
        }
    }
}
