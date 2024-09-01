using RedMaple.Orchestrator.Security.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Security.Builder
{
    public class CertificateSanBuilder
    {
        private CertificateBuilderData mCertificate;
        private SubjectAlternativeNameBuilder mSubjectAlternativeNameBuilder = new();

        internal SubjectAlternativeNameBuilder SubjectAlternativeNameBuilder => mSubjectAlternativeNameBuilder;

        internal CertificateSanBuilder(CertificateBuilderData certificate)
        {
            mCertificate = certificate;
        }

        public CertificateSanBuilder AddIpAddress(System.Net.IPAddress ipAddress)
        {
            mSubjectAlternativeNameBuilder.AddIpAddress(ipAddress);
            return this;
        }
        public CertificateSanBuilder AddDnsName(string dnsName)
        {
            mSubjectAlternativeNameBuilder.AddDnsName(dnsName);
            return this;
        }
    }
}
