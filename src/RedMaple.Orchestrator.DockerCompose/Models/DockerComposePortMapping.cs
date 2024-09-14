using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace RedMaple.Orchestrator.DockerCompose.Models
{
    /// <summary>
    /// Short syntax
    /// [HOST:]CONTAINER[/PROTOCOL]
    /// 
    /// Long Syntax:
    /// name: web
    /// target: 80
    /// host_ip: 127.0.0.1
    /// published: "8080"
    /// protocol: tcp
    /// app_protocol: http
    /// mode: host
    /// </summary>
    public class DockerComposePortMapping
    {
        public int? ContainerPort { get; set; }
        public int? HostPort { get; set; }
        public string? Name { get; set; }
        public string? HostIp { get; set; }
        public string? AppProtocol { get; set; }
        public string? Mode { get; set; }
        public string? Protocol { get; set; }
    }
}
