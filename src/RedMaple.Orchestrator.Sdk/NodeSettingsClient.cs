using RedMaple.Orchestrator.Contracts.Node;
using System.Net.Http.Json;

namespace RedMaple.Orchestrator.Sdk
{
    public class NodeSettingsClient : IDisposable
    {
        private readonly HttpClient _httpClient;

        public NodeSettingsClient(string baseUrl)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
        
        public async Task ApplySettingsAsync(NodeSettings settings)
        {
            string url = $"/api/settings";
            using var response = await _httpClient.PostAsJsonAsync(url, settings);
            response.EnsureSuccessStatusCode();
        }
        public async Task<NodeSettings> GetSettingsAsync()
        {
            string url = $"/api/settings";
            var settings = await _httpClient.GetFromJsonAsync<NodeSettings>(url);
            return settings ?? new();
        }
    }
}
