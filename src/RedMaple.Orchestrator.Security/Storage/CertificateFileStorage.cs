using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Security.Storage
{
    public class CertificateFileStorage : ICertificateFileStorage
    {
        public string GetRootDirectory()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return @"c:\temp\redmaple\controller\certs";
            }
            return "/data/redmaple/controller/certs";
        }
    }
}
