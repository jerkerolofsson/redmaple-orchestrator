using RedMaple.Orchestrator.Security.Provider;
using RedMaple.Orchestrator.Security.Services;
using RedMaple.Orchestrator.Security.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class CertificateServicesServiceExtensions
    {
        public static IServiceCollection AddCertificateAuthority(this IServiceCollection services)
        {
            services.AddTransient< ICertificateFileStorage, CertificateFileStorage > ();
            services.AddTransient<ICertificateProvider, CertificateProvider>();
            services.AddTransient<ICertificateAuthority,CertificateAuthority>();

            return services;
        }
    }
}
