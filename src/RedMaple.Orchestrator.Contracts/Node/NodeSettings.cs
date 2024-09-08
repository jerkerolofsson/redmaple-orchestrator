using RedMaple.Orchestrator.Contracts.Ingress;

namespace RedMaple.Orchestrator.Contracts.Node
{
    /// <summary>
    /// Settings for a node that can be defined by the controller
    /// </summary>
    public class NodeSettings
    {
        public bool EnableDns { get; set; }
    }
}
