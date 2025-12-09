
using RedMaple.Orchestrator.Contracts.Docker;

namespace RedMaple.Orchestrator.Sdk
{
    public interface INodeDockerDaemonClient
    {
        void Dispose();
        Task<List<DockerCertificate>> GetCertificatesAsync();
        Task SetCertificatesAsync(List<DockerCertificate> certs);
    }
}