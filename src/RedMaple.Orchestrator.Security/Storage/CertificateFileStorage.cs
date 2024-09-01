using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Security.Storage
{
    public class CertificateFileStorage : ICertificateFileStorage
    {
        public string GetRootDirectory()
        {
            return @"c:\temp\certs";
        }
    }
}
