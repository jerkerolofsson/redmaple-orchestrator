using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Security.Storage
{
    public interface ICertificateFileStorage
    {
        string GetRootDirectory();
    }
}
