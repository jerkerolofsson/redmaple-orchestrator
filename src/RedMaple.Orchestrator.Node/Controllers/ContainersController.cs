using Docker.DotNet.Models;
using Microsoft.AspNetCore.Mvc;

namespace RedMaple.Orchestrator.Node.Controllers
{
    [ApiController]
    [Route("/api/containers")]
    public class ContainersController : ControllerBase
    {
        private readonly ILogger<ContainersController> _logger;
        private readonly ILocalContainersClient _containersClient;

        public ContainersController(ILogger<ContainersController> logger, ILocalContainersClient containersClient)
        {
            _logger = logger;
            _containersClient = containersClient;
        }

        [HttpGet()]
        public async Task<List<Container>> GetContainersAsync([FromQuery] string? project)
        {
            var containers = await _containersClient.GetContainersAsync(project);

            return containers;
        }


        [HttpPost()]
        public async Task<CreateContainerResponse> CreateContainerAsyncAsync([FromBody] CreateContainerParameters createContainerParameters)
        {
            return await _containersClient.CreateContainerAsync(createContainerParameters);
        }

        [HttpGet("/api/containers/{id}/logs")]
        public async Task<IActionResult> GetLogsAsync(
            [FromRoute] string id,
            [FromQuery] string? tail,
            CancellationToken cancellationToken)
        {
            var result = new ContentStreamingResult(cancellationToken);
            await _containersClient.GetLogsAsync(id, follow:true, tail, result, result.CancellationTokenSource, cancellationToken);
            return result;
        }

        [HttpGet("/api/containers/{id}/stats")]
        public async Task<IActionResult?> GetStatsAsync([FromRoute] string id, CancellationToken cancellationToken)
        {
            var result = new ContentStreamingResult(cancellationToken);
            await _containersClient.GetStatsAsync(id, result, result.CancellationTokenSource, cancellationToken);
            return result;
        }

        [HttpPost("/api/containers/{id}/stop")]
        public async Task StopAsync([FromRoute] string id)
        {
            await _containersClient.StopAsync(id);
        }

        [HttpPost("/api/containers/{id}/start")]
        public async Task StartAsync([FromRoute] string id)
        {
            await _containersClient.StartAsync(id);
        }

        [HttpPost("/api/containers/{id}/restart")]
        public async Task RestartAsync([FromRoute] string id)
        {
            await _containersClient.StopAsync(id);
            await _containersClient.StartAsync(id);
        }
    }
}
