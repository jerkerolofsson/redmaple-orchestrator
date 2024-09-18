using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Domain.Metrics.Models
{
    public record class DoubleTime(DateTime Time, double Value);

    public class ContainerStats
    {
        private const int MaxPoints = 30;


        public required string ContainerId { get; set; }

        /// <summary>
        /// CPU Usage, 0.0 - 100.0
        /// </summary>
        public double CpuUsage { get; set; }

        public IReadOnlyCollection<DoubleTime> CpuHistory => _cpuHistory;

        private LinkedList<DoubleTime>  _cpuHistory = new();

        internal void SetCpuUsage(double cpuUsage)
        {
            CpuUsage = cpuUsage;

            while(CpuHistory.Count > MaxPoints)
            {
                _cpuHistory.RemoveFirst();
            }
            _cpuHistory.AddLast(new DoubleTime(DateTime.Now, cpuUsage));
        }
    }
}
