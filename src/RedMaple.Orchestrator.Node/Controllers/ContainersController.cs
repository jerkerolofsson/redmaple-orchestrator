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
        public async Task<List<Container>> GetContainersAsync()
        {
            return await _containersClient.GetContainersAsync();
        }


        [HttpPost()]
        public async Task<CreateContainerResponse> CreateContainerAsyncAsync([FromBody] CreateContainerParameters createContainerParameters)
        {
            return await _containersClient.CreateContainerAsync(createContainerParameters);
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
