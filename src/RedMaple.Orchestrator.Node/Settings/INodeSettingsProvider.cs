using RedMaple.Orchestrator.Contracts.Node;

namespace RedMaple.Orchestrator.Node.Settings
{
    public interface INodeSettingsProvider
    {
        Task ApplySettingsAsync(NodeSettings settings);
        Task<NodeSettings> GetSettingsAsync();
    }
}
