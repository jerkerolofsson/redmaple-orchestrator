using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.Contracts.Containers;
using RedMaple.Orchestrator.Contracts.Deployments;
using RedMaple.Orchestrator.Contracts.Node;
using RedMaple.Orchestrator.Controller.Domain.Deployments;
using RedMaple.Orchestrator.Controller.Domain.Metrics.ContainerMetrics.Models;
using RedMaple.Orchestrator.Controller.Domain.Node;
using RedMaple.Orchestrator.Sdk;
using System.Collections.Concurrent;
using System.Text.Json;

namespace RedMaple.Orchestrator.Controller.Domain.Metrics.ContainerMetrics
{
    /// <summary>
    /// Collects container statistics
    /// </summary>
    /// <param name="Logger"></param>
    /// <param name="Node"></param>
    /// <param name="ContainerId"></param>
    internal record class ContainerStatsCollector(
        ContainerStatsCollectorManager Owner,
        ILogger Logger,
        IContainerStatsManager StatsManager,
        NodeInfo Node,
        string ContainerId) : IProgress<string>, IDisposable
    {
        private CancellationTokenSource? _cancellationTokenSource;

        private ContainerStatsResponse? _prev;

        public Task? Task { get; private set; }

        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
        }

        public void Start(NodeContainersClient client)
        {
            _cancellationTokenSource ??= new CancellationTokenSource();
            this.Task = client.StartReadStatsTaskAsync(ContainerId, this, _cancellationTokenSource.Token);
        }

        public void Dispose()
        {
            Stop();
        }

        public void Report(string value)
        {
            try
            {
                var response = JsonSerializer.Deserialize<ContainerStatsResponse>(value);

                ReportCpuUsage(response);
                ReportMemoryUsage(response);

                Owner.OnContainerStatsUpdated(ContainerId);
                _prev = response;
            }
            catch (Exception)
            {

            }
        }

        private void ReportMemoryUsage(ContainerStatsResponse? response)
        {
            if (response?.MemoryStats?.Stats is not null)
            {
                var usage = response.MemoryStats.Usage;
                var limit = response.MemoryStats.Limit;
                if (usage is not null)
                {
                    StatsManager.ReportMemoryUsage(ContainerId, usage.Value, limit);
                }
            }
        }
        private void ReportCpuUsage(ContainerStatsResponse? response)
        {
            int numCores = 1;
            if (response?.CPUStats?.CPUUsage?.PercpuUsage is not null)
            {
                numCores = response.CPUStats.CPUUsage.PercpuUsage.Length;
            }
            double? cpuDelta = null;
            double? systemDelta = null;
            if (_prev?.CPUStats?.CPUUsage?.TotalUsage is not null &&
                response?.CPUStats?.CPUUsage?.TotalUsage is not null)
            {
                cpuDelta = (double)response.CPUStats.CPUUsage.TotalUsage.Value - (double)_prev.CPUStats.CPUUsage.TotalUsage.Value;
            }
            if (_prev?.CPUStats?.SystemUsage is not null &&
                response?.CPUStats?.SystemUsage is not null)
            {
                systemDelta = (double)response.CPUStats.SystemUsage.Value - (double)_prev.CPUStats.SystemUsage.Value;
            }
            if (cpuDelta is not null && systemDelta is not null)
            {
                double cpuPercent = 0;
                if (systemDelta > 0)
                {
                    cpuPercent = (cpuDelta.Value / systemDelta.Value) * numCores * 100.0;
                }
                Logger.LogTrace("Container: {ContainerId} CPU: {cpuPercent}%", ContainerId, Math.Round(cpuPercent, 2));
                cpuPercent = Math.Min(0, cpuPercent);
                StatsManager.ReportCpuUsage(ContainerId, cpuPercent);
            }
        }
    }

    internal class ContainerStatsCollectorManager : IContainerStatsCollectorManager, IContainerStatsUpdateProvider
    {
        private readonly IContainerStatsManager _statsManager;
        private readonly INodeManager _nodeManager;
        private readonly IDeploymentManager _deploymentManager;
        private readonly ILogger<ContainerStatsCollectorManager> _logger;
        private readonly ConcurrentDictionary<string, ContainerStatsCollector> _collectors = new();

        public event EventHandler<string>? ContainerStatsUpdated;

        public ContainerStatsCollectorManager(
            ILogger<ContainerStatsCollectorManager> logger,
            IDeploymentManager deploymentManager,
            INodeManager nodeManager,
            IContainerStatsManager statsManager)
        {
            _deploymentManager = deploymentManager;
            _logger = logger;
            _nodeManager = nodeManager;
            _statsManager = statsManager;
        }

        private ConcurrentDictionary<string, NodeContainersClient> _clients = new();
        public NodeContainersClient GetClient(string baseUrl)
        {
            if(_clients.TryGetValue(baseUrl, out var client))
            {
                return client;
            }
            client = new NodeContainersClient(baseUrl);
            _clients[baseUrl] = client;
            return client;
        }

        public async Task UpdateCollectorsForNodeAsync(NodeInfo node)
        {
            var client = GetClient(node.BaseUrl);
            var containers = await client.GetContainersAsync();
            foreach (var container in containers)
            {
                if (!container.IsRunning)
                {
                    StopCollection(container);
                }
                else
                {
                    StartCollection(node, client, container);
                }
            }
        }

        private void StopCollection(Container container)
        {
            if (_collectors.TryRemove(container.Id, out var collector))
            {
                _logger.LogInformation("Stopping collection on container {ContainerId}", container.Id);
                collector.Stop();
            }
        }

        private void StartCollection(NodeInfo node, NodeContainersClient client, Container container)
        {
            if (!_collectors.ContainsKey(container.Id))
            {
                // Start a new one
                _logger.LogInformation("Starting collection on container {ContainerId}", container.Id);
                var collector = new ContainerStatsCollector(this,_logger, _statsManager, node, container.Id);
                _collectors[container.Id] = collector;
                collector.Start(client);
            }
            else
            {
                // Restart if it has stopped
                var collector = _collectors[container.Id];
                if (collector.Task is null || collector.Task.IsCompleted == true)
                {
                    _logger.LogInformation("Resuming collection on container {ContainerId}", container.Id);
                    collector.Start(client);
                }
            }
        }

        public async Task UpdateCollectorsForDeploymentAsync(Deployment deployment)
        {
            var node = await _nodeManager.GetNodeByIpAddressAsync(deployment.ApplicationServerIp);
            if(node is not null)
            {
                await UpdateCollectorsForNodeAsync(node);
            }
        }

        internal void OnContainerStatsUpdated(string containerId)
        {
            ContainerStatsUpdated?.Invoke(this, containerId);
        }
    }
}
