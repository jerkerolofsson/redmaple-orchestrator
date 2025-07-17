using RedMaple.Orchestrator.Contracts.Deployments;
using RedMaple.Orchestrator.Contracts.Docker;

using System.Net.Http.Json;

namespace RedMaple.Orchestrator.Sdk
{
    public class NodeDockerDaemonClient : INodeDockerDaemonClient, IDisposable
    {
        private readonly HttpClient _httpClient;

        public NodeDockerDaemonClient(string baseUrl)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        public async Task SetCertificatesAsync(List<DockerCertificate> certs)
        {
            string url = $"/api/docker-daemon/certificates";
            using var response = await _httpClient.PostAsJsonAsync(url, certs);
            response.EnsureSuccessStatusCode();
        }
        public async Task<List<DockerCertificate>> GetCertificatesAsync()
        {
            string url = $"/api/docker-daemon/certificates";
            return await _httpClient.GetFromJsonAsync<List<DockerCertificate>>(url) ?? [];
        }
    }
}
