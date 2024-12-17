using RedMaple.Orchestrator.Contracts.Deployments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Secrets
{
    public interface ISecretsRepository
    {
        Task DeleteSecretAsync(Secret secret);
        Task<Secret?> GetSecretAsync(string id);
        Task<List<Secret>> GetSecretsAsync();
        Task SaveSecretAsync(Secret secret);
    }
}
