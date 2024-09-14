using RedMaple.Orchestrator.Contracts.Deployments;

namespace RedMaple.Orchestrator.Sdk
{
    public interface INodeDeploymentClient : IDisposable
    {
        Task AddDeploymentAsync(AddNodeDeploymentRequest plan, IProgress<string> progress);
        Task UpAsync(string slug, IProgress<string> progress);
        Task DownAsync(string slug, IProgress<string> progress);
        Task DeleteAsync(string slug, IProgress<string> progress);
    }
}