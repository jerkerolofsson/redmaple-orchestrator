using RedMaple.Orchestrator.Contracts.Dns;
using RedMaple.Orchestrator.Contracts.Ingress;
using RedMaple.Orchestrator.Contracts.Node;
using System.Net.Http.Json;

namespace RedMaple.Orchestrator.Sdk
{
    public class NodeIngressClient : IIngressServiceController, IDisposable
    {
        private readonly HttpClient _httpClient;

        public NodeIngressClient(string baseUrl)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        public async Task AddIngressServiceAsync(IngressServiceDescription serviceDescription, IProgress<string> progress)
        {
            string url = $"/api/ingress";
            using var response = await _httpClient.PostAsJsonAsync(url, serviceDescription);
            response.EnsureSuccessStatusCode();
            var text = await response.Content.ReadAsStringAsync();
            foreach(var line in text.Split('\n'))
            {
                progress.Report(line);
            }
        }
        public async Task DeleteIngressServiceAsync(string id)
        {
            string url = $"/api/ingress/{id}";
            using var response = await _httpClient.DeleteAsync(url);
            response.EnsureSuccessStatusCode();
        }
        public async Task<List<IngressServiceDescription>> GetServicesAsync()
        {
            string url = $"/api/ingress";
            var entries = await _httpClient.GetFromJsonAsync<List<IngressServiceDescription>>(url);
            return entries ?? new();
        }
    }
}
