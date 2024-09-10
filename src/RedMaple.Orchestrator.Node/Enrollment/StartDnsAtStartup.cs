
using RedMaple.Orchestrator.Contracts;
using System.Text;
using System.Net.Http.Json;
using System.Net;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using RedMaple.Orchestrator.Contracts.Node;
using RedMaple.Orchestrator.Ingress;

namespace RedMaple.Orchestrator.Node.Enrollment
{
    public class StartDnsAtStartup : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly INodeSettingsProvider _nodeSettingsProvider;
        private readonly IDns _dns;

        public StartDnsAtStartup(
            ILogger<StartDnsAtStartup> logger,
            INodeSettingsProvider nodeSettingsProvider,
            IDns dns)
        {
            _logger = logger;
            _nodeSettingsProvider = nodeSettingsProvider;
            _dns = dns;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var settings = await _nodeSettingsProvider.GetSettingsAsync();
            if (settings.EnableDns)
            {
                _logger.LogInformation("DNS is enabled");
                await _dns.StartAsync();
            }
            else
            {
                _logger.LogInformation("DNS is disabled");
            }
        }
    }
}
