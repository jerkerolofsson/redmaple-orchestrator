using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Domain.Metrics.Models
{
    public record class DoubleTime(DateTime Time, double Value);
    public record class LongTime(DateTime Time, long Value);

    public class ContainerStats
    {
        private const int MaxPoints = 30;

        public required string ContainerId { get; set; }

        /// <summary>
        /// CPU Usage, 0.0 - 100.0
        /// </summary>
        public double CpuPercent { get; set; }

        /// <summary>
        /// Memory Usage 0.0 - 100.0
        /// </summary>
        public double MemoryUsagePercent { get; set; }

        public long MemoryUsage { get; set; }
        public long? MemoryLimit { get; set; }

        public IReadOnlyCollection<DoubleTime> CpuPercentHistory => _cpuHistory.ToList();
        public IReadOnlyCollection<LongTime> MemoryUsageHistory => _memoryUsageHistory.ToList();
        public IReadOnlyCollection<DoubleTime> MemoryUsagePercentHistory => _memoryUsagePercentHistory.ToList();

        private LinkedList<DoubleTime> _cpuHistory = new();
        private LinkedList<LongTime> _memoryUsageHistory = new();
        private LinkedList<DoubleTime> _memoryUsagePercentHistory = new();

        internal void SetMemoryUsage(long usage, long? limit)
        {
            var now = DateTime.Now;
            if (limit is not null && limit != 0)
            {
                MemoryUsagePercent = usage / (double)limit.Value;
                MemoryLimit = limit.Value;
                _memoryUsagePercentHistory.AddLast(new DoubleTime(now, MemoryUsagePercent));
                while (_memoryUsagePercentHistory.Count > MaxPoints)
                {
                    _memoryUsagePercentHistory.RemoveFirst();
                }
            }
            MemoryUsage = usage;
            _memoryUsageHistory.AddLast(new LongTime(now, usage));

            while (_memoryUsageHistory.Count > MaxPoints)
            {
                _memoryUsageHistory.RemoveFirst();
            }
        }
        internal void SetCpuUsage(double cpuUsage)
        {
            CpuPercent = cpuUsage;

            while(_cpuHistory.Count > MaxPoints)
            {
                _cpuHistory.RemoveFirst();
            }
            _cpuHistory.AddLast(new DoubleTime(DateTime.Now, cpuUsage));
        }
    }
}
