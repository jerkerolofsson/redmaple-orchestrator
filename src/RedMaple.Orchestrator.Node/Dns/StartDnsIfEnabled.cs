global using RedMaple.Orchestrator.Node.Settings;

namespace RedMaple.Orchestrator.Node.Dns
{
    public class StartDnsIfEnabled : INotificationHandler<NodeSettingsChangedNotification>
    {
        private readonly ILogger _logger;
        private readonly IDns _dns;

        public StartDnsIfEnabled(
            ILogger<StartDnsIfEnabled> logger, 
            IDns dns)
        {
            _logger = logger;
            _dns = dns;
        }

        public async Task Handle(NodeSettingsChangedNotification notification, CancellationToken cancellationToken)
        {
            if(notification.Settings.EnableDns)
            {
                _logger.LogInformation("Starting dns as it is enabled");
                await _dns.StartAsync();
            }
        }
    }
}
