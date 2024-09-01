namespace RedMaple.Orchestrator.Dns
{
    public interface IDns
    {
        Task StartAsync();
        Task StopAsync();
        Task AddEntryAsync(string hostname, string ipAddress);
        Task RemoveEntryAsync(string hostname);
    }
}
