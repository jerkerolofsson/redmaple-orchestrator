using RedMaple.Orchestrator.Security.Builder;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace RedMaple.Orchestrator.Security.Serialization
{
    public class CertificateSerializer
    {
        public static X509Certificate2 Import(string filename)
        {
            return Import(filename, password: null);
        }
        public static X509Certificate2 Import(string filename, string? password)
        {
            var bytes = File.ReadAllBytes(filename);
            var cert = new X509Certificate2(bytes, password, keyStorageFlags: X509KeyStorageFlags.Exportable);
            return cert;
        }
        public static string ToCertBase64(X509Certificate2 cert)
        {
            var bytes = ToCert(cert);
            return Convert.ToBase64String(bytes);
        }
        public static string ToPemBase64(X509Certificate2 cert)
        {
            var bytes = ExportPrivateKeyToPem(cert);
            return Convert.ToBase64String(bytes);
        }
        public static byte[] ToCert(X509Certificate2 cert)
        {
            var bytes = cert.Export(X509ContentType.Cert);
            return bytes;
        }
        public static byte[] ToPkcs12(X509Certificate2 cert)
        {
            var bytes = cert.Export(X509ContentType.Pkcs12);
            return bytes;
        }
        public static void ToPemCertFile(X509Certificate2 cert, string filename, List<X509Certificate2> chain)
        {
            if (chain.Count == 0)
            {
                var text = cert.ExportCertificatePem();
                File.WriteAllText(filename, text);
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine(cert.ExportCertificatePem());
                foreach (var c in chain)
                {
                    sb.AppendLine(c.ExportCertificatePem());
                }

                //X509Certificate2Collection collection = [cert, .. chain];
                //var text = collection.ExportCertificatePems();
                File.WriteAllText(filename, sb.ToString());
            }
        }
        public static void ExportCertificatePems(string filename, params X509Certificate2[] certs)
        {
            var chain = new X509Certificate2Collection();
            foreach (var cert in certs)
            {
                chain.Add(cert);
            }
            var text = chain.ExportCertificatePems();

            // Change to this implementation when updating to dotnet7/8
            //var text = cert.ExportCertificatePem();
            //var certPem = cert.ExportCertificatePem();
            //var issuerPem = issuerCert.ExportCertificatePem();
            //var text = new string(PemEncoding.Write("CERTIFICATE", cert.RawData));
            //var text = certPem + "\n" + issuerPem;
            File.WriteAllText(filename, text);
        }
        public static void ToPemKeyFile(X509Certificate2 cert, string filename)
        {
            var bytes = ExportPrivateKeyToPem(cert);
            File.WriteAllBytes(filename, bytes);
        }

        public static void ToCertFile(X509Certificate2 cert, string filename, List<X509Certificate2> chain)
        {
            if (chain.Count == 0)
            {
                var bytes = ToCert(cert);
                File.WriteAllBytes(filename, bytes);
            }
            else
            {
                X509Certificate2Collection collection = [.. chain, cert];
                var bytes = collection.Export(X509ContentType.Cert);
                if (bytes is null)
                {
                    throw new Exception("FAiled to export cert");
                }
                File.WriteAllBytes(filename, bytes);
            }
        }
        public static void ToCertFile(X509Certificate2 cert, string filename, string password)
        {
            var bytes = cert.Export(X509ContentType.Cert, password);
            File.WriteAllBytes(filename, bytes);
        }
        public static byte[] ToPfx(X509Certificate2 cert)
        {

            var bytes = cert.Export(X509ContentType.Pfx);
            return bytes;
        }
        public static byte[] ToPfx(X509Certificate2 cert, string password)
        {
            var bytes = cert.Export(X509ContentType.Pfx, password);
            return bytes;
        }
        public static void ToPfxFile(X509Certificate2 cert, string filename, string password, List<X509Certificate2> chain)
        {
            if (chain.Count == 0)
            {
                var bytes = cert.Export(X509ContentType.Pfx, password);
                File.WriteAllBytes(filename, bytes);
            }
            else
            {
                X509Certificate2Collection collection = [cert, .. chain];
                var bytes = collection.Export(X509ContentType.Pfx, password);
                if (bytes is null)
                {
                    throw new Exception("Failed to export pfx");
                }
                File.WriteAllBytes(filename, bytes);
            }
        }
        public static void ToPfxFile(X509Certificate2 cert, string filename, List<X509Certificate2> chain)
        {
            if (chain.Count == 0)
            {
                var bytes = cert.Export(X509ContentType.Pfx);
                File.WriteAllBytes(filename, bytes);
            }
            else
            {
                X509Certificate2Collection collection = [cert, .. chain];
                var bytes = collection.Export(X509ContentType.Pfx, password:null);
                if (bytes is null)
                {
                    throw new Exception("FAiled to export pfx");
                }
                File.WriteAllBytes(filename, bytes);
            }
        }

        public static void ExportPrivateKeyToPemFile(X509Certificate2 cert, string filename)
        {
            if (!cert.HasPrivateKey)
            {
                throw new ArgumentException("This certiface does not contain a private key!");
            }

            if (cert.SignatureAlgorithm.Value == CertificateBuilder.OID_SIGNATURE_ALGO_RSA)
            {
                var rsaParameters = ExportRsaPrivateKey(cert);
                ExportRsaParametersToPemFile(rsaParameters, filename);
            }
            else if (cert.SignatureAlgorithm.Value == CertificateBuilder.OID_SIGNATURE_ALGO_ECDSA)
            {
                var ecdaPrivateKey = ExportECDsaPrivateKey(cert);
                ExportEcdsaParametersToPemFile(ecdaPrivateKey, filename);
            }
            else
            {
                throw new NotImplementedException($"Signature Algorithm export to PEM not implemented for {cert.SignatureAlgorithm.Value} ({cert.SignatureAlgorithm.FriendlyName})");
            }
        }

        private static RSAParameters ExportRsaPrivateKey(X509Certificate2 cert)
        {
            if (!cert.HasPrivateKey)
            {
                throw new InvalidOperationException("No private key");
            }
            using (RSA tmp = RSA.Create())
            using (RSA? key = cert.GetRSAPrivateKey())
            {
                if (key is null)
                {
                    throw new InvalidOperationException("Certificate has no private RSA key");
                }

                // https://github.com/dotnet/runtime/issues/26031
                /*
                 * Exportable ends up meaning two different things depending on if the key got loaded into Windows CAPI or Windows CNG. 
                 * For CAPI it means ... exportable -- ExportParameters will work, and exporting as a PFX will work. 
                 * For CNG it ends up meaning "exportable if encrypted", so PFX export works, and ExportEncryptedPkcs8PrivateKey works... 
                 * but ExportParameters and ExportPkcs8PrivateKey do not.
                 * */
                PbeParameters pbeParameters = new PbeParameters(PbeEncryptionAlgorithm.Aes128Cbc, HashAlgorithmName.SHA256, 1);
                var password = Guid.NewGuid().ToString();
                tmp.ImportEncryptedPkcs8PrivateKey(password, key.ExportEncryptedPkcs8PrivateKey(password, pbeParameters), out int _);
                return tmp.ExportParameters(true);
            }
        }
        private static ECDsa ExportECDsaPrivateKey(X509Certificate2 cert)
        {
            if (!cert.HasPrivateKey)
            {
                throw new InvalidOperationException("No private key");
            }
            var tmp = ECDsa.Create();
            using (var key = cert.GetECDsaPrivateKey())
            {
                if (key is null)
                {
                    throw new InvalidOperationException("Certificate has no ECDsa private key");
                }

                // https://github.com/dotnet/runtime/issues/26031
                /*
                 * Exportable ends up meaning two different things depending on if the key got loaded into Windows CAPI or Windows CNG. 
                 * For CAPI it means ... exportable -- ExportParameters will work, and exporting as a PFX will work. 
                 * For CNG it ends up meaning "exportable if encrypted", so PFX export works, and ExportEncryptedPkcs8PrivateKey works... 
                 * but ExportParameters and ExportPkcs8PrivateKey do not.
                 * */
                PbeParameters pbeParameters = new PbeParameters(PbeEncryptionAlgorithm.Aes128Cbc, HashAlgorithmName.SHA256, 1);
                var password = Guid.NewGuid().ToString();
                tmp.ImportEncryptedPkcs8PrivateKey(password, key.ExportEncryptedPkcs8PrivateKey(password, pbeParameters), out int _);
                return tmp;
            }
            //return cert.GetECDsaPrivateKey();
        }

        public static byte[] ExportPrivateKeyToPem(X509Certificate2 cert)
        {
            var rsaParameters = ExportRsaPrivateKey(cert);

            // Save in memory stream
            using var outputStream = new MemoryStream();
            ExportRsaParametersToPem(rsaParameters, outputStream);

            // Copy output from memorystream to byte[] and return it
            outputStream.Seek(0, SeekOrigin.Begin);
            var bytes = new byte[outputStream.Length];
            Array.Copy(outputStream.GetBuffer(), bytes, bytes.Length);
            return bytes;
        }

        private static void ExportEcdsaParametersToPemFile(ECDsa ecdsa, string filename)
        {
            using var outputStream = File.Create(filename);
            ExportEcdsaParametersToPem(ecdsa, outputStream);
        }
        private static void ExportEcdsaParametersToPem(ECDsa ecdsa, Stream outputStream)
        {
            byte[] privKeyBytes = ecdsa.ExportPkcs8PrivateKey();
            //ToPkcs12
            char[] privKeyPem = PemEncoding.Write("EC PRIVATE KEY", privKeyBytes);
            using var streamWriter = new StreamWriter(outputStream, Encoding.ASCII, leaveOpen: true);
            streamWriter.Write(privKeyPem);
            var temp = new string(privKeyPem);
        }
        private static void ExportRsaParametersToPemFile(RSAParameters rsaParameters, string filename)
        {
            using var outputStream = File.Create(filename);
            ExportRsaParametersToPem(rsaParameters, outputStream);
        }
        private static void ExportRsaParametersToPem(RSAParameters rsaParameters, Stream outputStream)
        {
            using (var stream = new MemoryStream())
            {
                var writer = new BinaryWriter(stream);
                writer.Write((byte)0x30); // SEQUENCE
                using (var innerStream = new MemoryStream())
                {
                    var innerWriter = new BinaryWriter(innerStream);
                    EncodeIntegerBigEndian(innerWriter, new byte[] { 0x00 }); // Version
                    EncodeIntegerBigEndian(innerWriter, rsaParameters.Modulus!);
                    EncodeIntegerBigEndian(innerWriter, rsaParameters.Exponent!);
                    EncodeIntegerBigEndian(innerWriter, rsaParameters.D!);
                    EncodeIntegerBigEndian(innerWriter, rsaParameters.P!);
                    EncodeIntegerBigEndian(innerWriter, rsaParameters.Q!);
                    EncodeIntegerBigEndian(innerWriter, rsaParameters.DP!);
                    EncodeIntegerBigEndian(innerWriter, rsaParameters.DQ!);
                    EncodeIntegerBigEndian(innerWriter, rsaParameters.InverseQ!);
                    var length = (int)innerStream.Length;
                    EncodeLength(writer, length);
                    writer.Write(innerStream.GetBuffer(), 0, length);
                }

                var base64 = Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length).ToCharArray();
                using var streamWriter = new StreamWriter(outputStream, Encoding.ASCII, leaveOpen: true);
                streamWriter.WriteLine("-----BEGIN RSA PRIVATE KEY-----");
                // Output as Base64 with lines chopped at 64 characters
                for (var i = 0; i < base64.Length; i += 64)
                {
                    streamWriter.WriteLine(base64, i, Math.Min(64, base64.Length - i));
                }
                streamWriter.WriteLine("-----END RSA PRIVATE KEY-----");
            }
        }

        private static void EncodeLength(BinaryWriter stream, int length)
        {
            if (length < 0) throw new ArgumentOutOfRangeException("length", "Length must be non-negative");
            if (length < 0x80)
            {
                // Short form
                stream.Write((byte)length);
            }
            else
            {
                // Long form
                var temp = length;
                var bytesRequired = 0;
                while (temp > 0)
                {
                    temp >>= 8;
                    bytesRequired++;
                }
                stream.Write((byte)(bytesRequired | 0x80));
                for (var i = bytesRequired - 1; i >= 0; i--)
                {
                    stream.Write((byte)(length >> 8 * i & 0xff));
                }
            }
        }

        private static void EncodeIntegerBigEndian(BinaryWriter stream, byte[] value, bool forceUnsigned = true)
        {
            stream.Write((byte)0x02); // INTEGER
            var prefixZeros = 0;
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] != 0) break;
                prefixZeros++;
            }
            if (value.Length - prefixZeros == 0)
            {
                EncodeLength(stream, 1);
                stream.Write((byte)0);
            }
            else
            {
                if (forceUnsigned && value[prefixZeros] > 0x7f)
                {
                    // Add a prefix zero to force unsigned if the MSB is 1
                    EncodeLength(stream, value.Length - prefixZeros + 1);
                    stream.Write((byte)0);
                }
                else
                {
                    EncodeLength(stream, value.Length - prefixZeros);
                }
                for (var i = prefixZeros; i < value.Length; i++)
                {
                    stream.Write(value[i]);
                }
            }
        }
    }
}
