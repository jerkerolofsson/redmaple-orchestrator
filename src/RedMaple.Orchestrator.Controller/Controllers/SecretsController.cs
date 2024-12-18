using Microsoft.AspNetCore.Mvc;

using RedMaple.Orchestrator.Contracts.Node;
using RedMaple.Orchestrator.Controller.Domain.Cluster;

namespace RedMaple.Orchestrator.Controller.Controllers
{
    [Route("api/secrets")]
    public class SecretsController : ControllerBase
    {
        private readonly ISecretsManager _secretsManager;

        public SecretsController(ISecretsManager secretsManager)
        {
            _secretsManager = secretsManager;
        }

        /// <summary>
        /// Returns the value of a secret, encrypted, as a base64 string
        /// </summary>
        /// <returns></returns>
        [HttpGet("/api/secrets/{secretId}/value")]
        public async Task<string> GetSecretValueAsync(string secretId)
        {
            var secret = await _secretsManager.GetSecretAsync(secretId);
            return secret?.Value ?? "";
        }
    }
}
