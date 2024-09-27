using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace RedMaple.Orchestrator.DockerCompose.Models
{
    /// <summary>
    /// </summary>
    public class DockerComposeDeviceReservation
    {
        public string? driver { get; set; }
        public List<string>? capabilities { get; set; }
        public List<string>? device_ids { get; set; }
        public Dictionary<string, string>? options { get; set; }
        public int? count { get; set; }
    }
}
