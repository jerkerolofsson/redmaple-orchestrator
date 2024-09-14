using RedMaple.Orchestrator.Contracts.Deployments;
namespace RedMaple.Orchestrator.Node.LocalDeployments
{
    public interface ILocalDeploymentService
    {
        Task AddDeploymentAsync(AddNodeDeploymentRequest plan, IProgress<string> progress);
        Task DeleteDeploymentAsync(string slug, IProgress<string> progress, CancellationToken cancellationToken);
        Task DownAsync(string slug, IProgress<string> progress, CancellationToken cancellationToken);
        Task UpAsync(string slug, IProgress<string> progress, CancellationToken cancellationToken);
    }
}