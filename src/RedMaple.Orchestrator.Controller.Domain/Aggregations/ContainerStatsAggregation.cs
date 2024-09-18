using RedMaple.Orchestrator.Contracts.Containers;
using RedMaple.Orchestrator.Contracts.Node;
using RedMaple.Orchestrator.Controller.Domain.Metrics.Models;

namespace RedMaple.Orchestrator.Controller.Domain.Aggregations
{
    public class ContainerStatsAggregation
    {
        public required Container Container { get; set; }
        public required NodeInfo Node { get; set; }
        public ContainerStats? Stats { get; set; }

        public string CpuUsageString
        {
            get
            {
                if(Stats is not null)
                {
                    return Math.Round(Stats.CpuUsage, 2) + "%";
                }
                return "";
            }
        }

    }
}
