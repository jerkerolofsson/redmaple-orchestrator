using RedMaple.Orchestrator.Contracts.Node;

namespace RedMaple.Orchestrator.Controller.Domain.Cluster
{
    public interface IClusterService
    {
        Task<bool> OnEnrollAsync(EnrollmentRequest request, string remoteIp);
    }
}