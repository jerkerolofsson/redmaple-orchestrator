namespace RedMaple.Orchestrator.Contracts.Dns
{
    public interface IDns
    {
        /// <summary>
        /// Starts the DNS server
        /// </summary>
        /// <returns></returns>
        Task StartAsync();

        /// <summary>
        /// Stops the DNS server
        /// </summary>
        /// <returns></returns>
        Task StopAsync();

        /// <summary>
        /// Adds an entry
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        Task AddEntryAsync(DnsEntry entry);

        /// <summary>
        /// Removes an entry
        /// </summary>
        /// <param name="hostname"></param>
        /// <returns></returns>
        Task RemoveEntryAsync(string hostname);

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
