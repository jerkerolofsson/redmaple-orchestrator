using RedMaple.Orchestrator.Contracts.Ingress;

namespace RedMaple.Orchestrator.Contracts.Node
{
    /// <summary>
    /// Settings for a node that can be defined by the controller
    /// </summary>
    public class NodeSettings
    {
        public string? Id { get; set; }

        /// <summary>
        /// Is DNS server enabled on the node
        /// </summary>
        public bool EnableDns { get; set; }

        /// <summary>
        /// Can the node host ingress services
        /// </summary>
        public bool IngressHost { get; set; }

        /// <summary>
        /// Can the node host load balancers
        /// </summary>
        public bool LoadBalancerHost { get; set; }

        /// <summary>
        /// Can the node host application services
        /// </summary>
        public bool ApplicationHost { get; set; }
    }
}
