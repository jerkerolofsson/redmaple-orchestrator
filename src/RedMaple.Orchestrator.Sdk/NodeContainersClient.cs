using Docker.DotNet;
using Docker.DotNet.Models;
using System.Net.Http;
using System;
using System.Net.Http.Json;
using RedMaple.Orchestrator.Contracts.Containers;

namespace RedMaple.Orchestrator.Sdk
{
    public class NodeContainersClient : IRemoteContainersClient
    {
        private readonly HttpClient _httpClient;

        public NodeContainersClient(string baseUrl)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        public Task ReadLogsAsync(string id, string? tail, IProgress<string> callback, CancellationToken cancellationToken)
        {
            string url = $"/api/containers/{id}/logs";
            if(tail is not null)
            {
                url += "?tail=" + tail;
            }
            return Task.Factory.StartNew(async () =>
            {
                try
                {
                    using var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, url), HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();
                    using var stream = response.Content.ReadAsStream();
                    using var reader = new StreamReader(stream);
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var line = await reader.ReadLineAsync(cancellationToken);
                        if (line is null)
                        {
                            break;
                        }
                        callback.Report(line);
                    }
                }
                catch { }
            }, TaskCreationOptions.LongRunning);

        }

        public async Task StartAsync(string id)
        {
            string url = $"/api/containers/{id}/start";
            using var response = await _httpClient.PostAsync(url, null);
            response.EnsureSuccessStatusCode();
        }
        public async Task StopAsync(string id)
        {
            string url = $"/api/containers/{id}/stop";
            using var response = await _httpClient.PostAsync(url, null);
            response.EnsureSuccessStatusCode();
        }

        public async Task RestartAsync(string id)
        {
            string url = $"/api/containers/{id}/restart";
            using var response = await _httpClient.PostAsync(url, null);
            response.EnsureSuccessStatusCode();
        }

        public async Task<CreateContainerResponse> CreateContainerAsync(CreateContainerParameters parameters)
        {
            string url = $"/api/containers";
            using var response = await _httpClient.PostAsJsonAsync(url, parameters);
            var createResponse = await response.Content.ReadFromJsonAsync<CreateContainerResponse>();
            return createResponse ?? new CreateContainerResponse
            {
                ID = null
            };
        }

        public async Task<List<Container>> GetContainersAsync()
        {
            string url = $"/api/containers";
            var containers = await _httpClient.GetFromJsonAsync<List<Container>>(url);
            if (containers is null)
            {
                throw new Exception("null returned from GetContainers end-point");
            }
            return containers;
        }
    }
}
