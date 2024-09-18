
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
    public class StartIngressAtStartup : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly LocalIngressServiceController _ingress;
        private readonly INodeSettingsProvider _settingsProvider;

        public StartIngressAtStartup(
            ILogger<StartIngressAtStartup> logger, 
            LocalIngressServiceController ingress, 
            INodeSettingsProvider settingsProvider)
        {
            _logger = logger;
            _ingress = ingress;
            _settingsProvider = settingsProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var settings = await _settingsProvider.GetSettingsAsync();
            if (settings.IngressHost)
            {
                _logger.LogInformation("Starting ingress..");
                await _ingress.StartAsync();
            }
        }
    }
}
