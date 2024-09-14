using RedMaple.Orchestrator.Contracts.Deployments;
using System.Net.Http.Json;

namespace RedMaple.Orchestrator.Sdk
{
    public class NodeDeploymentClient : INodeDeploymentClient
    {
        private readonly HttpClient _httpClient;

        public NodeDeploymentClient(string baseUrl)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        public async Task DeleteAsync(string slug, IProgress<string> progress)
        {
            string url = $"/api/deployments/{slug}";
            using var response = await _httpClient.DeleteAsync(url);
            response.EnsureSuccessStatusCode();
            var text = await response.Content.ReadAsStringAsync();
            foreach (var line in text.Split('\n'))
            {
                progress.Report(line);
            }
        }

        public async Task DownAsync(string slug, IProgress<string> progress)
        {
            string url = $"/api/deployments/{slug}/down";
            using var response = await _httpClient.PostAsync(url, null);
            response.EnsureSuccessStatusCode();
            var text = await response.Content.ReadAsStringAsync();
            foreach (var line in text.Split('\n'))
            {
                progress.Report(line);
            }
        }
        public async Task UpAsync(string slug, IProgress<string> progress)
        {
            string url = $"/api/deployments/{slug}/up";
            using var response = await _httpClient.PostAsync(url, null);
            response.EnsureSuccessStatusCode();
            var text = await response.Content.ReadAsStringAsync();
            foreach (var line in text.Split('\n'))
            {
                progress.Report(line);
            }
        }
        public async Task AddDeploymentAsync(AddNodeDeploymentRequest plan, IProgress<string> progress)
        {
            string url = $"/api/deployments";
            using var response = await _httpClient.PostAsJsonAsync(url, plan);
            response.EnsureSuccessStatusCode();
            var text = await response.Content.ReadAsStringAsync();
            foreach (var line in text.Split('\n'))
            {
                progress.Report(line);
            }
        }
    }
}
