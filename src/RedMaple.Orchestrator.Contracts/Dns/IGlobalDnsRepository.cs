namespace RedMaple.Orchestrator.Contracts.Dns
{
    public interface IGlobalDnsRepository
    {
        Task<List<DnsEntry>> GetDnsEntriesAsync();
        Task SetDnsEntriesAsync(List<DnsEntry> entries);
    }
}