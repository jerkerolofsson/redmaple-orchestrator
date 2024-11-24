using Docker.DotNet.Models;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.Contracts.Containers;
using RedMaple.Orchestrator.Contracts.Deployments;
using RedMaple.Orchestrator.Contracts.Dns;
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
using System.Numerics;
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

        public List<VolumeBind> GetMissingVolumeNames(DeploymentPlan plan)
        {
            List<VolumeBind> names = new();

            if (plan.Kind == DeploymentKind.DockerCompose)
            {
                try
                {
                    var composePlan = DockerComposeParser.ParseYaml(plan.Plan);
                    if (composePlan?.services is not null)
                    {
                        foreach (var service in composePlan.services.Values)
                        {
                           if (service?.volumes is not null)
                            {
                                foreach (var volume in service.volumes)
                                {
                                    if (volume.Source is null || volume.Target is null)
                                    {
                                        continue;
                                    }
                                    if (!volume.Source.Contains('/') && !volume.Source.Contains('\\') && !volume.Source.StartsWith('$'))
                                    {
                                        var name = volume.Source.TrimEnd(':');
                                        if (!composePlan.volumes.ContainsKey(name))
                                        {
                                            // See if volume is added to plan
                                            if (!plan.VolumeBinds.Where(x => x.Name == name).Any())
                                            {
                                                names.Add(new VolumeBind
                                                {
                                                    Name = name,
                                                    ContainerPath = volume.Target
                                                });
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                }
            }
            return names;
        }

        public async Task<ValidationResult> ValidatePlanAsync(DeploymentPlan plan, bool validateDomainName = true)
        {
            var plans = await GetDeploymentPlansAsync();
            var planNames = plans.Where(x=>x.Id != plan.Id).Select(x => x.Name).ToHashSet();
            var planSlugs = plans.Where(x => x.Id != plan.Id).Select(x => x.Slug).ToHashSet();
            var dnsEntries = (await _dns.GetDnsEntriesAsync()).Where(x => x.Hostname == plan.DomainName && (plan.Region is null || x.Region is null || x.Region == plan.Region)).ToList();
            var result = new ValidationResult();

            if (plan.CreateDnsEntry || plan.CreateIngress)
            {
                if (validateDomainName)
                {
                    if (string.IsNullOrWhiteSpace(plan.DomainName))
                    {
                        result.Errors.Add(new ValidationFailure("DomainName", $"Domain name is missing"));
                    }
                    else if (dnsEntries.Count > 0)
                    {
                        result.Errors.Add(new ValidationFailure("DomainName", $"The domain name is already in use"));
                    }
                }
            }
            if (planNames.Contains(plan.Name))
            {
                result.Errors.Add(new ValidationFailure("Name", $"The deployment name is already in use"));
            }
            if (planSlugs.Contains(plan.Slug))
            {
                result.Errors.Add(new ValidationFailure("Name", $"The deployment slug is already in use"));
            }

            foreach (var env in plan.EnvironmentVariables)
            {
                if(string.IsNullOrEmpty(env.Value))
                {
                    result.Errors.Add(new ValidationFailure("EnvironmentVariables", $"The environment variable '{env.Key}' is missing"));
                }
            }

            var missingVolumes = GetMissingVolumeNames(plan);
            foreach (var volumeBind in missingVolumes)
            {
                result.Errors.Add(new ValidationFailure("Volumes", $"The volume '{volumeBind.Name}' is missing"));
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
                            else
                            {
                                result.Errors.Add(new ValidationFailure("Plan", "Image is missing"));
                            }
                        }
                    }
                }
                catch (Exception ex) 
                {
                    result.Errors.Add(new ValidationFailure("Plan", "Failed to parse docker compose: " + ex.Message));
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

            if (plan.ApplicationServerIps.Any())
            {
                environmentVariables["REDMAPLE_APP_SERVER_IP"] = plan.ApplicationServerIps.First();
            }

            environmentVariables["REDMAPLE_APP_HTTPS_PEM_KEY_HOST_PATH"] = $"/data/redmaple/node/certificates/{plan.Slug}/key.pem";
            environmentVariables["REDMAPLE_APP_HTTPS_PEM_CERT_HOST_PATH"] = $"/data/redmaple/node/certificates/{plan.Slug}/cert.pem";

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
            await TakeDownAsync(plan, progress, cancellationToken);

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

        public async Task RestartAsync(DeploymentPlan plan, IProgress<string> progress, CancellationToken cancellationToken)
        {
            if(plan.ApplicationServerIps.Count == 0)
            {
                progress.Report("Error: No app server defined");
                _logger.LogError("No app server defined for {DeploymentSlug}", plan.Slug);
                throw new Exception("No app server defined");
            }
            var destinationNode = await _nodeManager.GetNodeByIpAddressAsync(plan.ApplicationServerIps[0]);
            if (destinationNode?.IpAddress is null)
            {
                progress.Report("Error: Application server offline");
                _logger.LogError("Application server {AppServerIp} for {DeploymentSlug} was not found", plan.ApplicationServerIps[0], plan.Slug);
                throw new Exception("Application server offline");
            }

            var appDeployments = await GetApplicationDeploymentsAsync(plan.Slug);
            foreach (var appDeployment in appDeployments)
            {
                // Gets the currently deployed server
                var currentNode = await GetApplicationHostAsync(appDeployment);
                if (currentNode is null)
                {
                    _logger.LogWarning("Application host was not found for deployment {DeploymentSlug}", appDeployment.Slug);
                }
                else
                {
                    try
                    {
                        await _mediator.Publish(new AppDeploymentStoppingNotification(appDeployment, plan));
                        await DownDeploymentOnTargetAsync(plan, progress, currentNode, cancellationToken);
                        await DeleteDeploymentFromTargetAsync(plan, progress, currentNode, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to bring down deployment {DeploymentSlug} on node: {Node}", appDeployment.Slug, currentNode.IpAddress);
                    }
                }

                // Check if ingress node has changed
                if (!await ValidateIngressNodeAsync(plan, progress))
                {
                    await CreateDnsAndIngressAsync(plan, progress);
                }

                try
                {
                    await AddDeploymentOnTargetAsync(plan, progress, destinationNode);

                    await _mediator.Publish(new AppDeploymentStartingNotification(appDeployment));
                    await UpDeploymentOnTargetAsync(plan, progress, destinationNode, cancellationToken);

                    await WaitUntilReadyzAsync(plan, appDeployment, progress, cancellationToken);
                    await _mediator.Publish(new AppDeploymentReadyNotification(appDeployment, plan));

                    plan.Up = true;

                    // Replace deployment record (in case ingress or application ip has changed..)

                    progress.Report($"Deployment record for {plan.Slug} saved for {destinationNode.IpAddress}");

                    // Delete previous deployment record
                    await _appDeploymentRepository.DeleteDeploymentAsync(appDeployment.Id);

                    var newDeploymentRecord = CreateAppDeploymentRecord(plan, destinationNode.IpAddress);
                    await _appDeploymentRepository.SaveDeploymentAsync(newDeploymentRecord);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to bring up deployment {DeploymentSlug} on node: {Node}", appDeployment.Slug, destinationNode.IpAddress);
                    throw;
                }
            }

            // Save state
            await _deploymentPlanRepository.SaveDeploymentPlanAsync(plan);
        }

        private async Task<bool> ValidateIngressNodeAsync(DeploymentPlan plan, IProgress<string> progress)
        {
            if (plan.CreateIngress)
            {
                if (string.IsNullOrEmpty(plan.DomainName))
                {
                    return true;
                }
                var entries = await _dns.GetDnsEntriesAsync();
                var entry = entries.Where(x => x.Hostname == plan.DomainName).FirstOrDefault();
                if (entry is null)
                {
                    progress.Report("DNS entry missing");
                    return false;
                }
                if (entry.IpAddress != plan.IngressServerIp)
                {
                    _logger.LogWarning("Deployment {DeploymentSlug} ingress server IP does not match DNS", plan.Slug);
                    progress.Report($"{entry.Hostname} target changed");
                    return false;
                }

                var ingressService = await _ingressManager.GetIngressServiceByDomainNameAsync(plan.DomainName, plan.Region);
                if(ingressService is null)
                {
                    _logger.LogWarning("Deployment {DeploymentSlug} ingress service '{DomainName}' is missing", plan.Slug, plan.DomainName);
                    progress.Report("Ingress service is missing");
                    return false;
                }
                if (plan.ApplicationServerIps is { } && plan.ApplicationServerIps.Count > 0)
                {
                    if (ingressService.DestinationIp != plan.ApplicationServerIps.First())
                    {
                        _logger.LogWarning("Deployment {DeploymentSlug} ingress destination IP is wrong", plan.Slug);
                        progress.Report("Ingress destination changed");
                        return false;
                    }
                }

                return true;
            }
            if(plan.CreateDnsEntry && plan.ApplicationServerIps is not null && plan.ApplicationServerIps.Count > 0)
            {
                var entries = await _dns.GetDnsEntriesAsync();
                var entry = entries.Where(x => x.Hostname == plan.DomainName).FirstOrDefault();
                if (entry is null)
                {
                    return false;
                }
                if (entry.IpAddress != plan.ApplicationServerIps.First())
                {
                    _logger.LogWarning("Deployment {DeploymentSlug} application server IP does not match DNS", plan.Slug);
                    return false;
                }
                return true;
            }
            return true;
        }

        public async Task TakeDownAsync(DeploymentPlan plan, IProgress<string> progress, CancellationToken cancellationToken)
        {
            if (plan.CreateIngress)
            {
                await _ingressManager.TryDeleteIngressServiceByDomainNameAsync(plan.DomainName, plan.Region);
            }
            else if(plan.CreateDnsEntry)
            {
                await _dns.TryDeleteDnsEntryByDomainNameAsync(plan.DomainName, plan.Region);
            }

            foreach (var deployment in await GetApplicationDeploymentsAsync(plan.Slug))
            {
                var targetNode = await GetApplicationHostAsync(deployment);
                if (targetNode is null)
                {
                    throw new ArgumentException("Application node not found: " + deployment.ApplicationServerIp);
                }

                await _mediator.Publish(new AppDeploymentStoppingNotification(deployment, plan));

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
            progress.Report("Validating plan..");
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
            Deployment appDeployment = CreateAppDeploymentRecord(plan, applicationServerIp);

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
            await _mediator.Publish(new AppDeploymentReadyNotification(appDeployment, plan));

            // Save state
            plan.Up = true;
            plan.Health = new() { Status = HealthStatus.Healthy };
            await _deploymentPlanRepository.SaveDeploymentPlanAsync(plan);
        }

        private static Deployment CreateAppDeploymentRecord(DeploymentPlan plan, string applicationServerIp)
        {
            var deploymentId = plan.Slug + "-" + Guid.NewGuid().ToString();
            var appDeployment = new Deployment(
                deploymentId,
                plan.Slug,
                plan.Resource,
                applicationServerIp,
                plan.ApplicationServerPort,
                plan.ApplicationProtocol,
                plan.EnvironmentVariables
            );
            appDeployment.VolumeBinds = plan.VolumeBinds;
            return appDeployment;
        }

        public async Task WaitUntilReadyzAsync(
            DeploymentPlan plan,
            Deployment applicationDeployment,
            IProgress<string> progress, CancellationToken cancellationToken)
        {
            var readyz = plan.HealthChecks.Where(x => x.Type == Contracts.Healthz.HealthCheckType.Readyz).ToList();
            if (readyz.Count == 0)
            {
                return;
            }

            TimeSpan timeout = TimeSpan.FromSeconds(30);
            var startTimestamp = Stopwatch.GetTimestamp();
            while(timeout > Stopwatch.GetElapsedTime(startTimestamp))
            {
                foreach (var check in readyz)
                {
                    var status = await _healthChecker.CheckLivezAsync(applicationDeployment, plan, cancellationToken);
                    progress.Report($"{plan.Slug} readyz health check: {status}");
                    if (status == HealthStatus.Healthy)
                    {
                        return;
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }

            progress.Report($"Failed when waiting for readyz for {plan.Slug}");
            throw new Exception($"Failed to verify that deployment is up (readyz health check) for {plan.Slug}");
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

            UpdateDeploymentFromYaml(plan);

            // Check ingress


            progress.Report($"Adding deployment on {targetNode.IpAddress}..");
            var deploymentClient = new NodeDeploymentClient(targetNode.BaseUrl);
            await deploymentClient.AddDeploymentAsync(new AddNodeDeploymentRequest
            {
                VolumeBinds = plan.VolumeBinds,
                EnvironmentVariables = plan.EnvironmentVariables,
                HttpsCertificatePfx = plan.ApplicationHttpsCertificatePfx,
                HttpsCertificatePemCert = plan.ApplicationHttpsPemCert ?? [],
                HttpsCertificatePemKey = plan.ApplicationHttpsPemKey ?? [],
                Kind = plan.Kind,
                Plan = plan.Plan,
                Slug = plan.Slug
            }, progress);
        }

        private static void UpdateDeploymentFromYaml(DeploymentPlan plan)
        {
            try
            {
                plan.Version = "latest";
                var composePlan = DockerComposeParser.ParseYaml(plan.Plan);
                if (composePlan?.services is not null)
                {
                    foreach (var service in composePlan.services.Values)
                    {
                        if(!string.IsNullOrEmpty(service.image))
                        {
                            var imageParts = service.image.Split(':');
                            if(imageParts.Length > 0)
                            {
                                plan.Version = imageParts[^1];
                                break;
                            }
                        }
                    }
                }
            }
            catch { }
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
            plan.ApplicationHttpsCertificatePfx = CertificateSerializer.ToPfx(cert, password, [issuer, root]);
            plan.ApplicationHttpsPemCert = CertificateSerializer.ExportCertificatePemsAsByteArray(cert, issuer, root); // nginx order
            plan.ApplicationHttpsPemKey = CertificateSerializer.ExportPrivateKeyToPem(cert);
        }

        private async Task CreateDnsAndIngressAsync(DeploymentPlan plan, IProgress<string> progress)
        {
            if (string.IsNullOrEmpty(plan.DomainName))
            {
                throw new ArgumentException(nameof(plan.DomainName));
            }
            var applicationServerIp = plan.ApplicationServerIps.First();

            // Check if ingress already exists..
            if (plan.CreateIngress)
            {
                var existingIngressService = await _ingressManager.GetIngressServiceByDomainNameAsync(plan.DomainName, plan.Region);
                if(existingIngressService is { })
                {
                    if(existingIngressService.DestinationIp == applicationServerIp &&
                        existingIngressService.DestinationPort == plan.ApplicationServerPort)
                    {
                        // Ingress already exists
                        return;
                    }
                }
            }

            // Todo: LB: required if multiple ApplicationServerIps are set
            // 

            // We have two scenarios here.
            // If we have an ingress node then we set the DNS to it
            // If not, we set the DNS directly to the application IP

            if (!plan.CreateIngress && plan.CreateDnsEntry)
            {
                var entries = await _dns.GetDnsEntriesAsync();
                var existingEntry = entries.FirstOrDefault(x => x.Hostname == plan.DomainName);
                var newEntry = new Contracts.Dns.DnsEntry { IsGlobal = true, Hostname = plan.DomainName, IpAddress = applicationServerIp };
                if (existingEntry is null)
                {
                    progress.Report($"Creating DNS entry {plan.Slug} for {plan.DomainName} to {applicationServerIp}..");
                    _logger.LogInformation("Deployment plan DNS entry created for {domainName}={new}", plan.DomainName, applicationServerIp);
                    await _dns.AddDnsEntriesAsync(newEntry);
                }
                else if(existingEntry.IpAddress != applicationServerIp)
                {
                    progress.Report($"Updating DNS for {plan.Slug} from {existingEntry.IpAddress} to {applicationServerIp}..");

                    _logger.LogWarning("Deployment plan DNS entry changed from {old} to {new}", existingEntry.IpAddress, applicationServerIp);
                    existingEntry.IpAddress = applicationServerIp;
                    await _dns.TryDeleteDnsEntryByDomainNameAsync(existingEntry.Hostname, existingEntry.Region);
                    await _dns.AddDnsEntriesAsync(newEntry);
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

                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));

                await _ingressManager.AddIngressServiceAsync(new IngressServiceDescription
                {
                    Region = plan.Region,
                    IngressIp = plan.IngressServerIp,
                    DomainName = plan.DomainName,
                    IngressPort = 443,
                    Scheme = plan.ApplicationProtocol,
                    Id = plan.Slug + "-ingress",
                    DestinationIp = applicationServerIp,
                    DestinationPort = plan.ApplicationServerPort.Value
                }, progress, cts.Token);
            }
        }

        public async Task<List<DeploymentPlan>> GetDeploymentPlansAsync()
        {
            return await _deploymentPlanRepository.GetDeploymentPlansAsync();
        }

        public async Task ChangeImageTagAsync(DeploymentPlan plan, string tag)
        {
            var composePlan = DockerComposeParser.ParseYaml(plan.Plan);
            bool changed = false;
            if (composePlan?.services is not null)
            {
                foreach (var service in composePlan.services.Values)
                {
                    if (!string.IsNullOrEmpty(service.image))
                    {
                        plan.Version = tag;
                        var index = service.image.LastIndexOf(':');
                        if(index == -1)
                        {
                            service.image = service.image + ':' + tag;
                        }
                        else
                        {
                            service.image = service.image.Substring(0,index) + ':' + tag;
                        }
                        changed = true;
                    }
                }

                if (changed)
                {
                    plan.Plan = DockerComposeParser.ToYaml(composePlan);
                    await SaveAsync(plan);
                }
            }
        }
    }
}
