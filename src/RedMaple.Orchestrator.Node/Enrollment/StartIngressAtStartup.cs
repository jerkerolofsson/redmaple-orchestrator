
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
        private ILogger _logger;
        private LocalIngressServiceController _ingress;

        public StartIngressAtStartup(ILogger<StartIngressAtStartup> logger, LocalIngressServiceController ingress)
        {
            _logger = logger;
            _ingress = ingress;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting ingress..");
            await _ingress.StartAsync();
        }
    }
}
