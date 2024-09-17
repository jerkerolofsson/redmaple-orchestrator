using RedMaple.Orchestrator.Controller.Domain.Metrics.Models;

namespace RedMaple.Orchestrator.Controller.Domain.Metrics.ContainerMetrics
{
    public interface IContainerStatsManager
    {
        event EventHandler<ContainerStats>? StatsUpdated;
        ContainerStats? GetContainerStats(string containerId);
        void ReportCpuUsage(string containerId, double cpuUsage);
    }
}