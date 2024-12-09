using System.Diagnostics;

using Microsoft.AspNetCore.Mvc;

using RedMaple.Orchestrator.Contracts.Deployments;
using RedMaple.Orchestrator.Contracts.Node;
using RedMaple.Orchestrator.Controller.Domain.Cluster;
using RedMaple.Orchestrator.Controller.Domain.Deployments;

namespace RedMaple.Orchestrator.Controller.Controllers
{
    [Route("api/deployments")]
    [ApiController]
    public class DeploymentController : ControllerBase
    {
        private readonly IDeploymentManager _deploymentManager;
        private readonly ILogger<DeploymentController> _logger;

        public DeploymentController(IDeploymentManager deploymentManager, ILogger<DeploymentController> logger)
        {
            _deploymentManager = deploymentManager;
            _logger = logger;
        }

        [HttpGet("/api/deployments")]
        public async Task<List<DeploymentPlan>> GetDeploymentPlansAsync(CancellationToken cancellationToken)
        {
            var plans = await _deploymentManager.GetDeploymentPlansAsync();
            return plans;
        }
        /// <summary>
        /// Restarts a deployment optionally changing the tag/version to deploy a new version
        /// </summary>
        /// <param name="deploymentSlug"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        [HttpPost("/api/deployments/{deploymentSlug}")]
        public async Task<IActionResult> DeployAsync([FromRoute] string deploymentSlug, [FromQuery] string? tag)
        {
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var progress = new ContentStreamingResult(cts.Token);

            var deployment = await _deploymentManager.GetDeploymentBySlugAsync(deploymentSlug);
            if (deployment == null)
            {
                return NotFound();
            }

            var _ = Task.Run(async () =>
            {
                try
                {
                    var startTimestamp = Stopwatch.GetTimestamp();
                    _logger.LogInformation("Deploy: Taking down {deploymentSlug}..", deploymentSlug);
                    await _deploymentManager.TakeDownAsync(deployment, progress, cancellationToken);

                    if (tag is not null)
                    {
                        _logger.LogInformation("Deploy: Changing tag to {tag} for {deploymentSlug}", tag, deploymentSlug);
                        await _deploymentManager.ChangeImageTagAsync(deployment, tag);
                    }

                    _logger.LogInformation("Deploy: Pulling images for {deploymentSlug}..", deploymentSlug);
                    await _deploymentManager.PullImagesAsync(deployment, progress, progress, cancellationToken);

                    _logger.LogInformation("Deploy: Bringing up {deploymentSlug}..", deploymentSlug);
                    await _deploymentManager.BringUpAsync(deployment, progress, cancellationToken);
                    _logger.LogInformation("Deploy: Restarted {deploymentSlug}!", deploymentSlug);

                    var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
                    _logger.LogInformation("Deploy: Deployment for {deploymentSlug} completed in {seconds} seconds", deploymentSlug, elapsed.TotalSeconds);

                    cts.Cancel();
                }
                catch { }
            }, cancellationToken);

            return progress;
        }
    }
}
