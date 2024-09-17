using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Domain.Metrics.ContainerMetrics.Models
{

    public class ContainerStatsResponse
    {
        public DateTime Read { get; set; }
        public DateTime PreRead { get; set; }
        public Pidsstats? PidsStats { get; set; }
        public Blkiostats? BlkioStats { get; set; }
        public long?  NumProcs { get; set; }
        public Storagestats? StorageStats { get; set; }
        public Cpustats? CPUStats { get; set; }
        public Precpustats? PreCPUStats { get; set; }
        public Memorystats? MemoryStats { get; set; }
        public string? Name { get; set; }
        public string? ID { get; set; }
        public Networks? Networks { get; set; }
    }

    public class Pidsstats
    {
        public long?  Current { get; set; }
        public long?  Limit { get; set; }
    }

    public class Blkiostats
    {
        /*
        public object[] IoServiceBytesRecursive { get; set; }
        public object[] IoServicedRecursive { get; set; }
        public object[] IoQueuedRecursive { get; set; }
        public object[] IoServiceTimeRecursive { get; set; }
        public object[] IoWaitTimeRecursive { get; set; }
        public object[] IoMergedRecursive { get; set; }
        public object[] IoTimeRecursive { get; set; }
        public object[] SectorsRecursive { get; set; }
        */
    }

    public class Storagestats
    {
        public long?  ReadCountNormalized { get; set; }
        public long?  ReadSizeBytes { get; set; }
        public long?  WriteCountNormalized { get; set; }
        public long?  WriteSizeBytes { get; set; }
    }

    public class Cpustats
    {
        public Cpuusage? CPUUsage { get; set; }
        public long? SystemUsage { get; set; }
        public long?  OnlineCPUs { get; set; }
        public Throttlingdata? ThrottlingData { get; set; }
    }

    public class Cpuusage
    {
        public long?  TotalUsage { get; set; }
        public long[]? PercpuUsage { get; set; }
        public long?  UsageInKernelmode { get; set; }
        public long?  UsageInUsermode { get; set; }
    }

    public class Throttlingdata
    {
        public long?  Periods { get; set; }
        public long?  ThrottledPeriods { get; set; }
        public long?  ThrottledTime { get; set; }
    }

    public class Precpustats
    {
        public Cpuusage? CPUUsage { get; set; }
        public long?  SystemUsage { get; set; }
        public long?  OnlineCPUs { get; set; }
        public Throttlingdata? ThrottlingData { get; set; }
    }

    public class Memorystats
    {
        public long?  Usage { get; set; }
        public long?  MaxUsage { get; set; }
        public Stats? Stats { get; set; }
        public long?  Failcnt { get; set; }
        public long? Limit { get; set; }
        public long?  Commit { get; set; }
        public long?  CommitPeak { get; set; }
        public long?  PrivateWorkingSet { get; set; }
    }

    public class Stats
    {
        public long?  active_anon { get; set; }
        public long?  active_file { get; set; }
        public long?  cache { get; set; }
        public long?  dirty { get; set; }
        public long? hierarchical_memory_limit { get; set; }
        public long? hierarchical_memsw_limit { get; set; }
        public long?  inactive_anon { get; set; }
        public long?  inactive_file { get; set; }
        public long?  mapped_file { get; set; }
        public long?  pgfault { get; set; }
        public long?  pgmajfault { get; set; }
        public long?  pgpgin { get; set; }
        public long?  pgpgout { get; set; }
        public long?  rss { get; set; }
        public long?  rss_huge { get; set; }
        public long?  total_active_anon { get; set; }
        public long?  total_active_file { get; set; }
        public long?  total_cache { get; set; }
        public long?  total_dirty { get; set; }
        public long?  total_inactive_anon { get; set; }
        public long?  total_inactive_file { get; set; }
        public long?  total_mapped_file { get; set; }
        public long?  total_pgfault { get; set; }
        public long?  total_pgmajfault { get; set; }
        public long?  total_pgpgin { get; set; }
        public long?  total_pgpgout { get; set; }
        public long?  total_rss { get; set; }
        public long?  total_rss_huge { get; set; }
        public long?  total_unevictable { get; set; }
        public long?  total_writeback { get; set; }
        public long?  unevictable { get; set; }
        public long?  writeback { get; set; }
    }

    public class Networks : Dictionary<string, NetworkAdapterStats>
    {
    }

    public class NetworkAdapterStats
    {
        public long?  RxBytes { get; set; }
        public long?  RxPackets { get; set; }
        public long?  RxErrors { get; set; }
        public long?  RxDropped { get; set; }
        public long?  TxBytes { get; set; }
        public long?  TxPackets { get; set; }
        public long?  TxErrors { get; set; }
        public long?  TxDropped { get; set; }
        //public object? EndpointID { get; set; }
        //public object InstanceID { get; set; }
    }

}
