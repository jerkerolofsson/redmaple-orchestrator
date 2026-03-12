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

        public long CurrentRxBytes { get; set; }
        public long CurrentTxBytes { get; set; }

        public long MemoryUsage { get; set; }
        public long? MemoryLimit { get; set; }

        public IReadOnlyCollection<DoubleTime> CpuPercentHistory => _cpuHistory.ToList();
        public IReadOnlyCollection<LongTime> MemoryUsageHistory => _memoryUsageHistory.ToList();
        public IReadOnlyCollection<DoubleTime> MemoryUsagePercentHistory => _memoryUsagePercentHistory.ToList();
        public IReadOnlyCollection<LongTime> NetworkRxBytes => _networkRxBytes.ToList();
        public IReadOnlyCollection<LongTime> NetworkTxBytes => _networkTxBytes.ToList();

        private readonly LinkedList<DoubleTime> _cpuHistory = new();
        private readonly LinkedList<LongTime> _memoryUsageHistory = new();
        private readonly LinkedList<DoubleTime> _memoryUsagePercentHistory = new();

        private readonly LinkedList<LongTime> _networkRxBytes = new();
        private readonly LinkedList<LongTime> _networkTxBytes = new();

        private long _prevRxBytes = 0;
        private long _prevTxBytes = 0;

        internal void SetNetworkUsage(long rxBytes, long txBytes)
        {
            var now = DateTime.Now;

            long accumulatedTx = txBytes;
            long accumulatedRx = rxBytes;

            // We save the delta, the accumulated is reported
            if (_networkRxBytes.Count > 0)
            {
                rxBytes = Math.Max(0, rxBytes - _prevRxBytes);
            }
            if (_networkRxBytes.Count > 0)
            {
                txBytes = Math.Max(0, txBytes - _prevTxBytes);
            }

            if(_prevRxBytes == 0 || _prevTxBytes == 0)
            {
                // Don't include the first sample..
                _prevRxBytes = accumulatedRx;
                _prevTxBytes = accumulatedTx;

                return;
            }

            _prevRxBytes = accumulatedRx;
            _prevTxBytes = accumulatedTx;

            _networkRxBytes.AddLast(new LongTime(now, rxBytes));
            _networkTxBytes.AddLast(new LongTime(now, txBytes));

            CurrentRxBytes = rxBytes;
            CurrentTxBytes = txBytes;

            while (_networkRxBytes.Count > MaxPoints)
            {
                _networkRxBytes.RemoveFirst();
            }
            while (_networkTxBytes.Count > MaxPoints)
            {
                _networkTxBytes.RemoveFirst();
            }
        }

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
