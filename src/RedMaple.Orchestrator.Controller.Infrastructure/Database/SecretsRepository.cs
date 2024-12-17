using RedMaple.Orchestrator.Contracts.Secrets;
using RedMaple.Orchestrator.Infrastructure;
using System.Runtime.InteropServices;

namespace RedMaple.Orchestrator.Controller.Infrastructure.Database
{
    internal class SecretsRepository : DocumentStore<Secret>, ISecretsRepository
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        protected override string SaveFilePath => GetConfigFilePath();

        private string GetLocalConfigDir()
        {
            string path = "/data/redmaple/controller/secrets";
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (isWindows)
            {
                path = @"C:\temp\redmaple\controller\secrets";
            }
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        private string GetConfigFilePath()
        {
            string path = GetLocalConfigDir();
            var filePath = Path.Combine(path, "secrets.json");
            return filePath;
        }
        public async Task<List<Secret>> GetSecretsAsync()
        {
            return await LoadAsync();
        }

        public async Task SaveSecretAsync(Secret secret)
        {
            await _semaphore.WaitAsync();
            try
            {
                var resources = await LoadAsync();
                resources.RemoveAll(x => x.Id == secret.Id);
                resources.Add(secret);
                await CommitAsync(resources);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task DeleteSecretAsync(Secret secret)
        {
            await _semaphore.WaitAsync();
            try
            {
                var resources = await LoadAsync();
                resources.RemoveAll(x => x.Id == secret.Id);
                await CommitAsync(resources);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<Secret?> GetSecretAsync(string id)
        {
            await _semaphore.WaitAsync();
            try
            {
                return await FindAsync(x => x.Id == id);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
