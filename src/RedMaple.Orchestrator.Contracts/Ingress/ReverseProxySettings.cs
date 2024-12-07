using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Ingress
{
    public class ReverseProxySettings
    {

        /// <summary>
        /// http2 will be enabled in the reverse-proxy
        /// </summary>
        public bool EnableHttp2 { get; set; }
    }
}
