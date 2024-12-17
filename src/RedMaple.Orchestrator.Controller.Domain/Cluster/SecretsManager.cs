using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using RedMaple.Orchestrator.Contracts.Secrets;

using YamlDotNet.Core.Tokens;

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RedMaple.Orchestrator.Controller.Domain.Cluster
{
    internal class SecretsManager : ISecretsManager
    {
        private readonly ISecretsRepository _db;

        public SecretsManager(ISecretsRepository db)
        {
            _db = db;
        }

        public Secret CreateSecret(string secretName)
        {
            var key = Environment.GetEnvironmentVariable("REDMAPLE_ENCR_KEY") ?? throw new Exception("REDMAPLE_ENCR_KEY not set");
            var slug = new Slugify.SlugHelper().GenerateSlug(secretName); ;
            return new Secret
            {
                Key = key,
                Name = secretName,
                Slug = slug,
                Id = slug
            };
        }

        public async Task EncryptSecretAsync(Secret secret, byte[] value)
        {
            var key = Environment.GetEnvironmentVariable("REDMAPLE_ENCR_KEY") ?? throw new Exception("REDMAPLE_ENCR_KEY not set");
            var keyPair = key.Split('/');
            if(keyPair.Length != 2)
            {
                throw new Exception("Expected REDMAPLE_ENCR_KEY two contain two items separated by slash");
            }
            var aesKey = keyPair[0];
            var iv = keyPair[1];

            await Task.Run(async () =>
            {
                var aes = Aes.Create();
                
                ICryptoTransform encryptor = aes.CreateEncryptor(Convert.FromHexString(aesKey), Convert.FromHexString(iv));
                using var memoryStream = new MemoryStream();
                using (Stream c = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    c.Write(value, 0, value.Length);
                }
                var result = memoryStream.ToArray();

                secret.Value = Convert.ToBase64String(result);
                await SaveSecretAsync(secret);  
            });
        }
        public async Task<byte[]> DecryptSecretAsync(Secret secret)
        {
            var key = Environment.GetEnvironmentVariable("REDMAPLE_ENCR_KEY") ?? throw new Exception("REDMAPLE_ENCR_KEY not set");
            var keyPair = key.Split('/');
            if (keyPair.Length != 2)
            {
                throw new Exception("Expected REDMAPLE_ENCR_KEY two contain two items separated by slash");
            }
            var aesKey = keyPair[0];
            var iv = keyPair[1];

            var result = Array.Empty<byte>();
            await Task.Run(async () =>
            {
                if (!string.IsNullOrEmpty(secret.Value))
                {
                    var cipherText = Convert.FromBase64String(secret.Value);

                    var aes = Aes.Create();
                    ICryptoTransform decryptor = aes.CreateDecryptor(Convert.FromHexString(aesKey), Convert.FromHexString(iv));

                    using var src = new MemoryStream(cipherText);
                    using var dest = new MemoryStream();
                    using var c = new CryptoStream(src, decryptor, CryptoStreamMode.Read);
                    await c.CopyToAsync(dest);
                    result = dest.ToArray();
                }
            });
            return result;
        }

        public async Task SaveSecretAsync(Secret secret)
        {
            ArgumentNullException.ThrowIfNull(secret.Slug);
            ArgumentNullException.ThrowIfNull(secret.Id);
            ArgumentNullException.ThrowIfNull(secret.Name);

            await _db.SaveSecretAsync(secret);
        }
        public async Task AddSecretAsync(Secret secret)
        {
            ArgumentNullException.ThrowIfNull(secret.Name);

            if (string.IsNullOrEmpty(secret.Slug))
            {
                secret.Slug = new Slugify.SlugHelper().GenerateSlug(secret.Name);
                secret.Id = secret.Slug;
            }

            await _db.SaveSecretAsync(secret);
        }

        public async Task<Secret?> GetSecretAsync(string id)
        {
            return await _db.GetSecretAsync(id);
        }

        public async Task<List<Secret>> GetSecretsAsync()
        {
            return await _db.GetSecretsAsync();
        }
    }
}
