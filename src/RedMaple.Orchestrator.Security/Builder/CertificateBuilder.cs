using RedMaple.Orchestrator.Security.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace RedMaple.Orchestrator.Security.Builder
{
    public class CertificateBuilder
    {
        private readonly CertificateBuilderData mCertificate = new();
        private readonly List<CertificateSanBuilder> mSanBuilders = new();

        private readonly HashAlgorithmName mHashAlgorithmName = HashAlgorithmName.SHA256;
        private readonly RSASignaturePadding mRsaSignaturePadding = RSASignaturePadding.Pkcs1;

        public const string OID_TLS_WEB_SERVER_AUTHENTICATION = "1.3.6.1.5.5.7.3.1";
        public const string OID_TLS_WEB_CLIENT_AUTHENTICATION = "1.3.6.1.5.5.7.3.2";
        public const string OID_CODE_SIGNING = "1.3.6.1.5.5.7.3.3";
        public const string OID_EMAIL_PROTECTION = "1.3.6.1.5.5.7.3.4";
        public const string OID_TIME_STAMPING = "1.3.6.1.5.5.7.3.8";
        public const string OID_OCSP_SIGNING = "1.3.6.1.5.5.7.3.9";
        public const string OID_IPSEC_END_SYSTEM = " 1.3.6.1.5.5.7.3.5";
        public const string OID_IPSEC_TUNNEL = "1.3.6.1.5.5.7.3.6";
        public const string OID_IPSEC_USER = "1.3.6.1.5.5.7.3.7";

        public const string OID_SIGNATURE_ALGO_RSA = "1.2.840.113549.1.1.11";
        public const string OID_SIGNATURE_ALGO_ECDSA = "1.2.840.10045.4.3.2";

        public CertificateBuilder()
        {
        }

        public CertificateBuilder AsCertificateAuthority()
        {
            mCertificate.Usages = X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign;
            mCertificate.IsCertificateAuthority = true;
            return this;
        }

        public CertificateBuilder WithOids(IEnumerable<string> oids)
        {
            mCertificate.Oids ??= new();
            foreach (var oid in oids)
            {
                mCertificate.Oids.Add(oid);
            }
            return this;
        }
        public CertificateBuilder WithOid(string oid)
        {
            mCertificate.Oids ??= new();
            mCertificate.Oids.Add(oid);
            return this;
        }

        public CertificateBuilder WithStartValidity(DateTime dateTime)
        {
            mCertificate.ValidityStartTime = dateTime;
            return this;
        }
        public CertificateBuilder WithExpiry(TimeSpan expiry)
        {
            mCertificate.Expiry = expiry;
            return this;
        }

        public CertificateBuilder UseRSA()
        {
            mCertificate.SignatureAlgorithm = SignatureAlgorithm.RSA;
            return this;
        }
        public CertificateBuilder UseECDsa()
        {
            mCertificate.SignatureAlgorithm = SignatureAlgorithm.ECDsa;
            return this;
        }


        public CertificateBuilder AddStateOrProvince(string s)
        {
            mCertificate.StateOrProvinceNames.Add(s);
            return this;
        }
        public CertificateBuilder AddLocality(string l)
        {
            mCertificate.Localities.Add(l);
            return this;
        }
        public CertificateBuilder AddCommonName(string cn)
        {
            mCertificate.CommonNames.Add(cn);
            return this;
        }
        public CertificateBuilder AddOrganization(string o)
        {
            mCertificate.Organizations.Add(o);
            return this;
        }
        public CertificateBuilder AddCountry(string c)
        {
            mCertificate.Countries.Add(c);
            return this;
        }
        public CertificateBuilder SetPassword(string password)
        {
            mCertificate.Password = password;
            return this;
        }

        public CertificateBuilder WithSubjectAlternativeName(Action<CertificateSanBuilder> configurator)
        {
            var builder = new CertificateSanBuilder(mCertificate);

            mSanBuilders.Add(builder);

            configurator(builder);
            return this;
        }

        public CertificateBuilder AddOrganizationalUnit(string ou)
        {
            if (mCertificate.OrganizationalUnits == null)
            {
                mCertificate.OrganizationalUnits = new();
            }
            mCertificate.OrganizationalUnits.Add(ou);
            return this;
        }

        private string BuildDistinguishedName()
        {
            if (mCertificate.CommonNames.Count == 0)
            {
                throw new InvalidOperationException("CommonName is not set");
            }

            var dn = new StringBuilder();
            if (mCertificate.CommonNames != null)
            {
                foreach (var attribute in mCertificate.CommonNames)
                {
                    dn.Append(", CN=" + attribute);
                }
            }
            if (mCertificate.Organizations != null)
            {
                foreach (var attribute in mCertificate.Organizations)
                {
                    dn.Append(", O=" + attribute);
                }
            }
            if (mCertificate.OrganizationalUnits != null)
            {
                foreach (var ou in mCertificate.OrganizationalUnits)
                {
                    dn.Append(", OU=" + ou);
                }
            }
            if (mCertificate.Localities != null)
            {
                foreach (var ou in mCertificate.Localities)
                {
                    dn.Append(", L=" + ou);
                }
            }
            if (mCertificate.StateOrProvinceNames != null)
            {
                foreach (var ou in mCertificate.StateOrProvinceNames)
                {
                    dn.Append(", S=" + ou);
                }
            }
            if (mCertificate.Countries != null)
            {
                foreach (var ou in mCertificate.Countries)
                {
                    dn.Append(", C=" + ou);
                }
            }

            return dn.ToString().TrimStart(',').Trim();
        }

        public X509Certificate2 BuildSelfSignedCertificate()
        {
            X500DistinguishedName distinguishedName = new X500DistinguishedName(BuildDistinguishedName());

            using var rsa = RSA.Create(2048);
            using var ecdsa = ECDsa.Create();

            SignatureAlgorithm signatureAlgorithm = mCertificate.SignatureAlgorithm;
            CertificateRequest request = CreateCertificateRequest(signatureAlgorithm, distinguishedName, rsa, ecdsa);
            request.CertificateExtensions.Add(new X509KeyUsageExtension(mCertificate.Usages, false));

            var oidCollection = new OidCollection();
            foreach (var oid in mCertificate.Oids)
            {
                oidCollection.Add(new Oid(oid));
            }
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(oidCollection, true));

            if (mSanBuilders != null)
            {
                foreach (var sanBuilder in mSanBuilders)
                {
                    request.CertificateExtensions.Add(sanBuilder.SubjectAlternativeNameBuilder.Build());
                }
            }

            if (mCertificate.IsCertificateAuthority)
            {
                request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, true, 1, true));
            }

            var start = mCertificate.ValidityStartTime;
            var end = mCertificate.ValidityStartTime + mCertificate.Expiry;

            var certificate = request.CreateSelfSigned(new DateTimeOffset(start), new DateTimeOffset(end));
#if WINDOWS
            if (mCertificate.FriendlyName != null)
            {
                certificate.FriendlyName = mCertificate.FriendlyName;
            }
            else
            {
                certificate.FriendlyName = mCertificate.CommonName;
            }
#endif
            string? password = null;

            var cert = new X509Certificate2(certificate.Export(X509ContentType.Pfx, mCertificate.Password), password, X509KeyStorageFlags.Exportable);
            return cert;
        }

        private CertificateRequest CreateCertificateRequest(SignatureAlgorithm signatureAlgorithm, X500DistinguishedName distinguishedName, RSA rsa, ECDsa ecdsa)
        {
            CertificateRequest request;
            switch (signatureAlgorithm)
            {
                case SignatureAlgorithm.RSA:
                    {
                        request = new CertificateRequest(distinguishedName, rsa, mHashAlgorithmName, mRsaSignaturePadding);
                    }
                    break;
                case SignatureAlgorithm.ECDsa:
                    {
                        request = new CertificateRequest(distinguishedName, ecdsa, mHashAlgorithmName);
                    }
                    break;
                default:
                    throw new NotImplementedException($"Signature algorithm {signatureAlgorithm} is not implemented");
            }

            return request;
        }

        public X509Certificate2 BuildSignedCertificate(X509Certificate2 issuerCert)
        {
            SignatureAlgorithm signatureAlgorithm = mCertificate.SignatureAlgorithm;

            X500DistinguishedName distinguishedName = new X500DistinguishedName(BuildDistinguishedName());

            using var rsa = RSA.Create(2048);
            using var ecdsa = ECDsa.Create();

            CertificateRequest request = CreateCertificateRequest(signatureAlgorithm, distinguishedName, rsa, ecdsa);
            request.CertificateExtensions.Add(new X509KeyUsageExtension(mCertificate.Usages, true));

            var oidCollection = new OidCollection();
            foreach (var oid in mCertificate.Oids)
            {
                oidCollection.Add(new Oid(oid));
            }
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(oidCollection, true));

            if (mCertificate.IsCertificateAuthority)
            {
                request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, true, 1, true));
            }

            if (mSanBuilders != null)
            {
                foreach (var sanBuilder in mSanBuilders)
                {
                    request.CertificateExtensions.Add(sanBuilder.SubjectAlternativeNameBuilder.Build());
                }
            }
            var start = mCertificate.ValidityStartTime.ToUniversalTime();
            var end = start + mCertificate.Expiry;
            if (start > issuerCert.NotAfter)
            {
                throw new InvalidDataException("The issuer cert expiry is after the start date of the new certificate being created");
            }

            if (end > issuerCert.NotAfter.ToUniversalTime())
            {
                end = issuerCert.NotAfter.ToUniversalTime().AddDays(-1);
            }

            var uniqueId = Guid.NewGuid();
            var certificate = request.Create(issuerCert, new DateTimeOffset(start), new DateTimeOffset(end), uniqueId.ToByteArray());

            if (signatureAlgorithm == SignatureAlgorithm.RSA)
            {
                return certificate.CopyWithPrivateKey(rsa);
            }
            else if (signatureAlgorithm == SignatureAlgorithm.ECDsa)
            {
                return certificate.CopyWithPrivateKey(ecdsa);
            }
            return certificate;
        }
    }
}
