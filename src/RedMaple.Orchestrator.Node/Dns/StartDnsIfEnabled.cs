global using RedMaple.Orchestrator.Node.Settings;

namespace RedMaple.Orchestrator.Node.Dns
{
    public class StartDnsIfEnabled : INotificationHandler<NodeSettingsChangedNotification>
    {
        private readonly IDns _dns;

        public StartDnsIfEnabled(IDns dns)
        {
            _dns = dns;
        }

        public async Task Handle(NodeSettingsChangedNotification notification, CancellationToken cancellationToken)
        {
            if(notification.Settings.EnableDns)
            {
                await _dns.StartAsync();
            }
        }
    }
}
