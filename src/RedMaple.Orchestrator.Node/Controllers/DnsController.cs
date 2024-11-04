
using static Grpc.Core.Metadata;

namespace RedMaple.Orchestrator.Node.Controllers
{
    [ApiController]
    [Route("/api/dns")]
    public class DnsController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IDns _dns;
        private readonly INodeSettingsProvider _nodeSettingsProvider;

        public DnsController(
            ILogger<DnsController> logger,
            INodeSettingsProvider nodeSettingsProvider,
            IDns dns)
        {
            _logger = logger;
            _nodeSettingsProvider = nodeSettingsProvider;
            _dns = dns;
        }

        [HttpGet("/api/dns/table")]
        public async Task<List<DnsEntry>> GetDnsEntriesAsync()
        {
            return await _dns.GetDnsEntriesAsync();
        }


        [HttpPost("/api/dns/table")]
        public async Task SetDnsEntriesAsync([FromBody] List<DnsEntry> entries, CancellationToken cancellationToken)
        {
            await _dns.SetDnsEntriesAsync(entries);
            var settings = await _nodeSettingsProvider.GetSettingsAsync();
            if (settings.EnableDns)
            {
                _logger.LogInformation("DNS table updated, restarting dns..");

                // docker exec dnsmasq killall -HUP dnsmasq
                await _dns.ReloadConfigurationAsync(cancellationToken);
                //await _dns.StopAsync();
                //await _dns.StartAsync();
            }
        }

        [HttpPost("/api/dns/entries")]
        public async Task AddEntryAsync([FromBody] DnsEntry entry)
        {
            await _dns.AddEntryAsync(entry);
        }

        [HttpDelete("/api/dns/entries/{hostname}")]
        public async Task RemoveEntryAsync([FromRoute] string hostname)
        {
            await _dns.RemoveEntryAsync(hostname);
        }
    }
}
