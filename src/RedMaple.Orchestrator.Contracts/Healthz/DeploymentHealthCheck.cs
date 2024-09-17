using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Healthz
{
    /// <summary>
    /// Describes how to check for health of a running deployment.
    /// A deployment can have many checks.
    /// 
    /// Checks can target different IP addresses (Ingress, LoadBalancer, Application). 
    /// This is controlled by the Target property.
    /// 
    /// 
    /// </summary>
    public class DeploymentHealthCheck
    {
        public static DeploymentHealthCheck TcpLivez => new DeploymentHealthCheck()
        {
            RelativeUrl = "livez",
            Method = HealthCheckMethod.TcpConnect,
            Type = HealthCheckType.Livez,
            Target = HealthCheckTarget.Application
        };

        public static DeploymentHealthCheck DefaultApplicationLivez => new DeploymentHealthCheck()
        {
            RelativeUrl = "livez",
            Method = HealthCheckMethod.HttpGet,
            Type = HealthCheckType.Livez,
            Target = HealthCheckTarget.Application
        };
        public static DeploymentHealthCheck DefaultApplicationReadyz => new DeploymentHealthCheck()
        {
            RelativeUrl = "readyz",
            Method = HealthCheckMethod.HttpGet,
            Type = HealthCheckType.Readyz,
            Target = HealthCheckTarget.Application
        };

        /// <summary>
        /// Check interval, in seconds
        /// </summary>
        public double Interval { get; set; } = 10;

        /// <summary>
        /// Timeout interval, in seconds
        /// </summary>
        public double Timeout { get; set; } = 5;

        /// <summary>
        /// How to check for health. 
        /// This corresponds to the implementation of the health checker.
        /// </summary>
        public HealthCheckMethod Method { get; set; }

        /// <summary>
        /// What type of health is checked (ready, live)
        /// </summary>
        public HealthCheckType Type { get; set; }

        /// <summary>
        /// Which node is targeted
        /// </summary>
        public HealthCheckTarget Target { get; set; }

        /// <summary>
        /// Relative URL
        /// </summary>
        public string? RelativeUrl { get; set; }

        public int HealthyStatusCode { get; set; } = 200;
        public int DegradedStatusCode { get; set; } = 218;
        public int UnhealthyStatusCode { get; set; } = 503;

    }
}
