using Microsoft.Extensions.DependencyInjection;
using RedMaple.Orchestrator.Security.Provider;
using RedMaple.Orchestrator.Security.Services;
using RedMaple.Orchestrator.Security.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CertificateTests.CertificateTestFramework;

namespace CertificateTests
{
    internal class CertificateTestFramework : IDisposable
    {
        private readonly TempCertificateFileStorage mTempCertificateFileStorage = new();
        private ServiceProvider _serviceProvider;

        public class TempCertificateFileStorage : ICertificateFileStorage, IDisposable
        {
            private string mDir;

            public TempCertificateFileStorage()
            {
                mDir = Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
                if(!Directory.Exists(mDir))
                {
                    Directory.CreateDirectory(mDir);
                }
            }

            public void Dispose()
            {
                Directory.Delete(mDir, true);
            }

            public void Clean()
            {
                Directory.Delete(mDir, true);
                Directory.CreateDirectory(mDir);
            }

            public string GetRootDirectory()
            {
                return mDir;
            }
        }

        public CertificateTestFramework()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddCertificateAuthority();
            services.AddSingleton<ICertificateFileStorage>(mTempCertificateFileStorage);
            _serviceProvider = services.BuildServiceProvider();

        }

        public ICertificateAuthority CA => _serviceProvider.GetRequiredService<ICertificateAuthority>();
        public ICertificateProvider Provider => _serviceProvider.GetRequiredService<ICertificateProvider>();

        public void Dispose()
        {
            mTempCertificateFileStorage.Dispose();
        }
    }
}
