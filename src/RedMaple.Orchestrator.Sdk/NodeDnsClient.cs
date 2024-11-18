using RedMaple.Orchestrator.Contracts.Dns;
using RedMaple.Orchestrator.Contracts.Node;
using System.Net.Http.Json;

namespace RedMaple.Orchestrator.Sdk
{
    public class NodeDnsClient : IDisposable
    {
        private readonly HttpClient _httpClient;

        public NodeDnsClient(string baseUrl)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
        
        /// <summary>
        /// Sets DNS entries for the specified node
        /// </summary>
        /// <param name="entries"></param>
        /// <returns></returns>
        public async Task SetDnsEntriesAsync(List<DnsEntry> entries)
        {
            string url = $"/api/dns/table";
            using var response = await _httpClient.PostAsJsonAsync(url, entries);
            response.EnsureSuccessStatusCode();
        }
        public async Task<List<DnsEntry>> GetDnsEntriesAsync()
        {
            string url = $"/api/dns/table";
            var entries = await _httpClient.GetFromJsonAsync<List<DnsEntry>>(url);
            return entries ?? new();
        }
    }
}
