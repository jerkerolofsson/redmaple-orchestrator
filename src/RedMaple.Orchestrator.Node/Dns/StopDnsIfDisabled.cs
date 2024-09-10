global using RedMaple.Orchestrator.Node.Settings;

namespace RedMaple.Orchestrator.Node.Dns
{
    public class StopDnsIfDisabled : INotificationHandler<NodeSettingsChangedNotification>
    {
        private readonly ILogger _logger;
        private readonly IDns _dns;

        public StopDnsIfDisabled(
            ILogger<StopDnsIfDisabled> logger,
            IDns dns)
        {
            _logger = logger;
            _dns = dns;
        }

        public async Task Handle(NodeSettingsChangedNotification notification, CancellationToken cancellationToken)
        {
            if(!notification.Settings.EnableDns)
            {
                _logger.LogInformation("Stopping dns as it is disabled");
                await _dns.StopAsync();
            }
        }
    }
}
