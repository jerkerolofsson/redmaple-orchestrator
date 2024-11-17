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
        private readonly CertificateBuilderData _certificate;
        private readonly SubjectAlternativeNameBuilder _subjectAlternativeNameBuilder = new();

        internal SubjectAlternativeNameBuilder SubjectAlternativeNameBuilder => _subjectAlternativeNameBuilder;

        internal CertificateSanBuilder(CertificateBuilderData certificate)
        {
            _certificate = certificate;
        }

        public CertificateSanBuilder AddIpAddress(System.Net.IPAddress ipAddress)
        {
            _subjectAlternativeNameBuilder.AddIpAddress(ipAddress);
            return this;
        }
        public CertificateSanBuilder AddDnsName(string dnsName)
        {
            _subjectAlternativeNameBuilder.AddDnsName(dnsName);
            return this;
        }
    }
}
