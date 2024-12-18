using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Sdk
{
    public class SecretsClient : IDisposable
    {
        private readonly string _baseUrl;
        private readonly string _decryptionKey;
        private readonly byte[] _key;
        private readonly byte[] _iv;
        private readonly HttpClient _httpClient;

        public SecretsClient(string? baseUrl, string? decryptionKey)
        {
            _baseUrl = baseUrl ?? Environment.GetEnvironmentVariable("REDMAPLE_CONTROLLER") ?? throw new ArgumentException("REDMAPLE_CONTROLLER missing");
            _decryptionKey = decryptionKey ?? Environment.GetEnvironmentVariable("REDMAPLE_ENCR_KEY") ?? throw new ArgumentException("REDMAPLE_ENCR_KEY missing");

            var keyPair = _decryptionKey.Split('/');
            if (keyPair.Length != 2)
            {
                throw new Exception("Expected REDMAPLE_ENCR_KEY two contain two items separated by slash");
            }
            _key = Convert.FromHexString(keyPair[0]);
            _iv = Convert.FromHexString(keyPair[1]);

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_baseUrl);
        }

        /// <summary>
        /// Gets and decrypts the secret identified by the ID
        /// </summary>
        /// <param name="secretId"></param>
        /// <returns></returns>
        public async Task<byte[]> GetSecretAsync(string secretId)
        {
            var base64Encrypted = await _httpClient.GetStringAsync($"/api/secrets/{secretId}/value");
            var encryptedBytes = Convert.FromBase64String(base64Encrypted);

            var aes = Aes.Create();
            ICryptoTransform decryptor = aes.CreateDecryptor(_key, _iv);

            using var src = new MemoryStream(encryptedBytes);
            using var dest = new MemoryStream();
            using var c = new CryptoStream(src, decryptor, CryptoStreamMode.Read);
            await c.CopyToAsync(dest);
            return dest.ToArray();
        }

        public void Dispose()
        {

            _httpClient.Dispose();
        }
    }
}
