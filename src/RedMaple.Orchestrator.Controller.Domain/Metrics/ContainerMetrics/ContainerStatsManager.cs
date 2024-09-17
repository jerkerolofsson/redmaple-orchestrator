using RedMaple.Orchestrator.Controller.Domain.Metrics.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Domain.Metrics.ContainerMetrics
{
    /// <summary>
    /// Contains in memory statistics about a container
    /// </summary>
    internal class ContainerStatsManager : IContainerStatsManager
    {
        private readonly ConcurrentDictionary<string, ContainerStats> _stats = new ConcurrentDictionary<string, ContainerStats>();

        public event EventHandler<ContainerStats>? StatsUpdated;

        public void ReportCpuUsage(string containerId, double cpuUsage)
        {
            if (_stats.TryGetValue(containerId, out var stats))
            {
                stats.CpuUsage = cpuUsage;
            }
            else
            {
                stats = new ContainerStats { ContainerId = containerId, CpuUsage = cpuUsage };
                _stats[containerId] = stats;
            }

            StatsUpdated?.Invoke(this, stats);
        }

        public ContainerStats? GetContainerStats(string containerId)
        {
            _stats.TryGetValue(containerId, out ContainerStats? containerStats);
            return containerStats;
        }
    }
}
