namespace RedMaple.Orchestrator.Contracts.Containers
{
    public record class Container
    {
        public required string Id { get; set; }
        public required string? Name { get; set; }
        public string? Status { get; set; }
        public string? State { get; set; }
        public bool IsRunning { get; set; }
        public List<ContainerPort> Ports { get; set; } = new();
    }
}
