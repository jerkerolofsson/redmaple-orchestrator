using Docker.DotNet.Models;
using Microsoft.AspNetCore.Mvc;
using RedMaple.Orchestrator.Contracts.Deployments;
using RedMaple.Orchestrator.Node.LocalDeployments;
using System.Numerics;

namespace RedMaple.Orchestrator.Node.Controllers
{
    [ApiController]
    [Route("/api/deployments")]
    public class DeploymentsController : ControllerBase
    {
        private readonly ILogger<DeploymentsController> _logger;
        private readonly ILocalDeploymentService _service;

        public DeploymentsController(ILogger<DeploymentsController> logger, ILocalDeploymentService service)
        {
            _logger = logger;
            _service = service;
        }


        [HttpPost()]
        public async Task<string> AddDeploymentAsync([FromBody] AddNodeDeploymentRequest plan)
        {
            var results = new ProgressResultBuilder();
            await _service.AddDeploymentAsync(plan, results);
            return results.ToString() ?? "";
        }


        [HttpDelete("/api/deployments/{slug}")]
        public async Task<string> DeleteDeploymentAsync([FromRoute] string slug, CancellationToken cancellationToken)
        {
            var results = new ProgressResultBuilder();
            await _service.DeleteDeploymentAsync(slug, results, cancellationToken);
            return results.ToString() ?? "";
        }
        [HttpPost("/api/deployments/{slug}/down")]
        public async Task<string> DownAsync([FromRoute] string slug, CancellationToken cancellationToken)
        {
            var results = new ProgressResultBuilder();
            await _service.DownAsync(slug, results, cancellationToken);
            return results.ToString() ?? "";
        }

        [HttpPost("/api/deployments/{slug}/up")]
        public async Task<string> UpAsync([FromRoute] string slug, CancellationToken cancellationToken)
        {
            var results = new ProgressResultBuilder();
            await _service.UpAsync(slug, results, cancellationToken);
            return results.ToString() ?? "";
        }
    }
}