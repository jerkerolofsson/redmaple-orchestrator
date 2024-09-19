using RedMaple.Orchestrator.Controller.Domain.Metrics.Models;

namespace RedMaple.Orchestrator.Controller.Domain.Metrics.ContainerMetrics
{
    public interface IContainerStatsManager
    {
        ContainerStats? GetContainerStats(string containerId);
        void ReportCpuUsage(string containerId, double cpuUsage);
        void ReportMemoryUsage(string containerId, long usage, long? limit);
    }
}