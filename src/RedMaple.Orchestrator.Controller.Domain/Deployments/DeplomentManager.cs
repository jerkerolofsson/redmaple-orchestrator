using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.Contracts.Deployments;
using RedMaple.Orchestrator.Contracts.Ingress;
using RedMaple.Orchestrator.Contracts.Node;
using RedMaple.Orchestrator.Controller.Domain.Domain;
using RedMaple.Orchestrator.Controller.Domain.GlobalDns;
using RedMaple.Orchestrator.Controller.Domain.Ingress;
using RedMaple.Orchestrator.Controller.Domain.Node;
using RedMaple.Orchestrator.DockerCompose;
using RedMaple.Orchestrator.Sdk;
using RedMaple.Orchestrator.Security.Builder;
using RedMaple.Orchestrator.Security.Serialization;
using RedMaple.Orchestrator.Security.Services;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Cryptography;

namespace RedMaple.Orchestrator.Controller.Domain.Deployments
{
    internal class DeplomentManager : IDeplomentManager
    {
        private readonly IGlobalDns _dns;
        private readonly IDomainService _domain;
        private readonly string mSecretCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890";
        private readonly IIngressManager _ingressManager;
        private readonly INodeManager _nodeManager;
        private readonly ICertificateAuthority _ca;
        private readonly IDeploymentPlanRepository _deploymentPlanRepository;
        private readonly ILogger<DeplomentManager> _logger;

        public DeplomentManager(
            IDomainService domain, 
            IIngressManager ingressManager, 
            INodeManager nodeManager, 
            ICertificateAuthority certificateAuthority, 
            IDeploymentPlanRepository deploymentPlanRepository, 
            IGlobalDns dns, 
            ILogger<DeplomentManager> logger)
        {
            _domain = domain;
            _ingressManager = ingressManager;
            _nodeManager = nodeManager;
            _ca = certificateAuthority;
            _deploymentPlanRepository = deploymentPlanRepository;
            _dns = dns;
            _logger = logger;
        }

        public ValidationResult ValidatePlan(DeploymentPlan plan)
        {
            var result = new ValidationResult();

            foreach(var env in plan.EnvironmentVariables)
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
            if (string.IsNullOrEmpty(plan.ApplicationServerIp))
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
            environmentVariables["REDMAPLE_APP_HTTPS_CERTIFICATE_PASSWORD"] = RandomNumberGenerator.GetString(mSecretCharacters, 32);

            // Forward some system environment variables
            environmentVariables["OIDC_AUTHORITY"] = System.Environment.GetEnvironmentVariable("OIDC_AUTHORITY") ?? "";

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
            ArgumentNullException.ThrowIfNull(plan.ApplicationServerIp);
            var targetNode = await _nodeManager.GetNodeByIpAddressAsync(plan.ApplicationServerIp);
            if (targetNode is null)
            {
                throw new ArgumentException("Application node not found: " + plan.ApplicationServerIp);
            }

            await DeleteDeploymentFromTargetAsync(plan, progress, targetNode, cancellationToken);
            await _deploymentPlanRepository.DeleteDeploymentPlanAsync(plan.Id);
        }

        public async Task TakeDownAsync(DeploymentPlan plan, IProgress<string> progress, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(plan.ApplicationServerIp);
            var targetNode = await _nodeManager.GetNodeByIpAddressAsync(plan.ApplicationServerIp);
            if (targetNode is null)
            {
                throw new ArgumentException("Application node not found: " + plan.ApplicationServerIp);
            }

            await DownDeploymentOnTargetAsync(plan, progress, targetNode, cancellationToken);
        }

        public async Task BringUpAsync(DeploymentPlan plan, IProgress<string> progress, CancellationToken cancellationToken)
        {
            ValidatePlan(plan);
            ArgumentNullException.ThrowIfNull(plan.ApplicationServerIp);
            ArgumentNullException.ThrowIfNull(plan.ApplicationHttpsCertificatePassword);
            var targetNode = await _nodeManager.GetNodeByIpAddressAsync(plan.ApplicationServerIp);
            if (targetNode is null)
            {
                throw new ArgumentException("Application node not found: " + plan.ApplicationServerIp);
            }

            if (plan.CreateDnsEntry)
            {
                await CreateDnsAndIngressAsync(plan, progress);
            }
            else if (plan.CreateIngress)
            {
                throw new InvalidDataException("CreateDnsEntry is false but CreateIngress is true. This combination is not valid.");
            }

            // Generate pfx certificate for the application server
            if (plan.ApplicationHttpsCertificatePfx is null || plan.ApplicationHttpsCertificatePfx.Length == 0)
            {
                await GenerateHttpsCertificateAsync(plan, progress, cancellationToken);
                await _deploymentPlanRepository.SaveDeploymentPlanAsync(plan);
            }

            await AddDeploymentOnTargetAsync(plan, progress, targetNode);
            await UpDeploymentOnTargetAsync(plan, progress, targetNode, cancellationToken);
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
            ArgumentNullException.ThrowIfNullOrEmpty(plan.ApplicationServerIp);
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
                    if (plan.ApplicationServerIp is not null)
                    { 
                        sanBuilder.AddIpAddress(IPAddress.Parse(plan.ApplicationServerIp));
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
            if (string.IsNullOrEmpty(plan.ApplicationServerIp))
            {
                throw new ArgumentException(nameof(plan.ApplicationServerIp));
            }

            // We have two scenarios here.
            // If we have an ingress node then we set the DNS to it
            // If not, we set the DNS directly to the application IP

            if (!plan.CreateIngress)
            {
                var entries = await _dns.GetDnsEntriesAsync();
                var existingEntry = entries.FirstOrDefault(x => x.Hostname == plan.DomainName);
                if(existingEntry is null)
                {
                    progress.Report($"Creating DNS entry {plan.Slug} for {plan.DomainName} to {plan.ApplicationServerIp}..");
                    _logger.LogInformation("Deployment plan DNS entry creeated for {domainName}={new}", plan.DomainName, plan.ApplicationServerIp);
                    entries.Add(new Contracts.Dns.DnsEntry { IsGlobal = true, Hostname = plan.DomainName, IpAddress = plan.ApplicationServerIp});
                    await _dns.SetDnsEntriesAsync(entries);
                }
                else if(existingEntry.IpAddress != plan.ApplicationServerIp)
                {
                    progress.Report($"Updating DNS for {plan.Slug} from {existingEntry.IpAddress} to {plan.ApplicationServerIp}..");

                    _logger.LogWarning("Deployment plan DNS entry changed from {old} to {new}", existingEntry.IpAddress, plan.ApplicationServerIp);
                    existingEntry.IpAddress = plan.ApplicationServerIp;
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
                    DestinationIp = plan.ApplicationServerIp,
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
