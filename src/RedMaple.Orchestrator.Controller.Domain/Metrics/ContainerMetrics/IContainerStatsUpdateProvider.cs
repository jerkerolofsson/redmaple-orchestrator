using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.Contracts.Node;
using RedMaple.Orchestrator.Sdk;

namespace RedMaple.Orchestrator.Controller.Domain.Metrics.ContainerMetrics
{
    public interface IContainerStatsUpdateProvider
    {
        event EventHandler<string>? ContainerStatsUpdated;
   }
}