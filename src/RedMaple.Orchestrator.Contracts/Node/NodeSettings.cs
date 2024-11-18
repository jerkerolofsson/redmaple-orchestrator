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
        /// Add upstream DNS server on DNS server
        /// </summary>
        public string? UpstreamDns1 { get; set; }

        /// <summary>
        /// Add upstream DNS server on DNS server
        /// </summary>
        public string? UpstreamDns2 { get; set; }

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

        /// <summary>
        /// Tags set for the node
        /// </summary>
        public string Tags { get; set; } = "";

        /// <summary>
        /// Name of the node
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Geographical region
        /// Can be used to determine as a filter when selecting nodes from
        /// </summary>
        public string Region { get; set; } = "";
    }
}
