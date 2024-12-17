
using RedMaple.Orchestrator.Contracts.Secrets;

namespace RedMaple.Orchestrator.Controller.Domain.Cluster
{
    public interface ISecretsManager
    {
        Task<Secret?> GetSecretAsync(string id);
        Task<List<Secret>> GetSecretsAsync();
        Task AddSecretAsync(Secret secret);
        Secret CreateSecret(string secretName);
        Task SaveSecretAsync(Secret secret);
        Task EncryptSecretAsync(Secret secret, byte[] value);
        Task<byte[]> DecryptSecretAsync(Secret secret);
    }
}