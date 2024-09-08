
using static Grpc.Core.Metadata;

namespace RedMaple.Orchestrator.Node.Controllers
{
    [ApiController]
    [Route("/api/dns")]
    public class DnsController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IDns _dns;

        public DnsController(ILogger<DnsController> logger, IDns dns)
        {
            _logger = logger;
            _dns = dns;
        }

        [HttpGet("/api/dns/table")]
        public async Task<List<DnsEntry>> GetDnsEntriesAsync()
        {
            return await _dns.GetDnsEntriesAsync();
        }


        [HttpPost("/api/dns/table")]
        public async Task SetDnsEntriesAsync([FromBody] List<DnsEntry> entries)
        {
            await _dns.SetDnsEntriesAsync(entries);
            await _dns.StopAsync();
            await _dns.StartAsync();
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
