﻿
using RedMaple.Orchestrator.Contracts;
using System.Text;
using System.Net.Http.Json;
using System.Net;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using RedMaple.Orchestrator.Contracts.Node;

namespace RedMaple.Orchestrator.Node.Enrollment
{
    public class EnrollAtStartup : BackgroundService
    {
        private readonly IServer _server;
        private readonly HttpClient _httpClient;
        private readonly ILogger<EnrollAtStartup> _logger;
        private readonly INodeSettingsProvider _settingsProvider;

        public EnrollAtStartup(
            IServer server, 
            HttpClient httpClient, 
            ILogger<EnrollAtStartup> logger, 
            INodeSettingsProvider settingsProvider)
        {
            _server = server;
            _httpClient = httpClient;
            _logger = logger;
            _settingsProvider = settingsProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var settings = await _settingsProvider.GetSettingsAsync();
            if(string.IsNullOrWhiteSpace(settings.Id))
            {
                settings.Id = Guid.NewGuid().ToString();
            }
            if (!int.TryParse(Environment.GetEnvironmentVariable("NODE_PORT"), out int port))
            {
                port = 1889;
            }

            if (!int.TryParse(Environment.GetEnvironmentVariable("INGRESS_HTTPS_PORT"), out int ingressHttpsPort))
            {
                ingressHttpsPort = 443;
            }

            _logger.LogInformation("ENROLLING node with id={NodeId}, API Port={port}, Ingress Port={ingressHttpsPort}", settings.Id, port, ingressHttpsPort);

            await _settingsProvider.ApplySettingsAsync(settings);

            await Task.Delay(TimeSpan.FromSeconds(2));

            while (!stoppingToken.IsCancellationRequested)
            {
                string schema = "http";
                // Connect to controller
                try
                {
                    List<string> hostAddresses = [];
                    if(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NODE_IP")))
                    {
                        hostAddresses = [Environment.GetEnvironmentVariable("NODE_IP") ??"", .. hostAddresses];
                    }

                    if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                    {
                        hostAddresses = ["127.0.0.1"];
                    }

                    var nodeInfo = new EnrollmentRequest()
                    {
                        Id = settings.Id ?? throw new Exception("No Id specified"),
                        Schema = schema,
                        HostAddresses = hostAddresses, 
                        Port = port,
                        IngressHttpsPort = ingressHttpsPort,
                        Name = settings.Name,
                        Region =settings.Region,
                        IsDnsEnabled = settings.EnableDns,
                        IsIngressEnabled = settings.IngressHost,
                        IsLoadBalancerEnabled = settings.LoadBalancerHost,
                        IsApplicationHostEnabled = settings.ApplicationHost,
                        Tags = settings.Tags
                    };

                    using var response = await _httpClient.PostAsJsonAsync("http://controller/api/nodes", nodeInfo);
                    response.EnsureSuccessStatusCode();
                    _logger.LogInformation("Successfully enrolled with controller");

                    await Task.Delay(TimeSpan.FromSeconds(30));
                    settings = await _settingsProvider.GetSettingsAsync();
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex,"Failed to enroll with controller");
                }
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
    }
}
