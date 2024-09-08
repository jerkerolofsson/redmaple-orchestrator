
namespace RedMaple.Orchestrator.Node.Settings
{
    public record class NodeSettingsChangedNotification(NodeSettings Settings) : INotification;
}
