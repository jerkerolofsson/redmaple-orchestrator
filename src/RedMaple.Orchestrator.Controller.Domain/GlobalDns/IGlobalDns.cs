using RedMaple.Orchestrator.Contracts.Dns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Domain.GlobalDns
{
    /// <summary>
    /// Global DNS records are updated on all nodes
    /// </summary>
    public interface IGlobalDns
    {
        /// <summary>
        /// Replaces the complete address DNS table with the specified entries
        /// </summary>
        /// <param name="entries"></param>
        /// <returns></returns>
        Task SetDnsEntriesAsync(List<DnsEntry> entries);

        /// <summary>
        /// Returns all entries
        /// </summary>
        /// <returns></returns>
        Task<List<DnsEntry>> GetDnsEntriesAsync();
    }
}
