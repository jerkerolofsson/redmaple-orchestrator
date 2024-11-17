using Microsoft.AspNetCore.Mvc;
using RedMaple.Orchestrator.Contracts.Node;
using RedMaple.Orchestrator.Controller.Domain.Cluster;
using RedMaple.Orchestrator.Controller.Domain.Node;
using RedMaple.Orchestrator.Sdk;
using System.Net;

namespace RedMaple.Orchestrator.Controller.Controllers
{
    [Route("api/nodes")]
    [ApiController]
    public class NodeController : ControllerBase
    {
        private readonly ILogger<NodeController> _logger;
        private readonly INodeManager _repository;
        private readonly IClusterService _clusterService;

        public NodeController(
            ILogger<NodeController> logger,
            INodeManager repository,
            IClusterService clusterService)
        {
            _logger = logger;
            _repository = repository;
            _clusterService = clusterService;
        }

        /// <summary>
        /// Returns a list of all enrolled nodes
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<List<NodeInfo>> GetNodesAsync()
        {
            return await _repository.GetNodesAsync();
        }

        /// <summary>
        /// Enrolls a new node
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> EnrollAsync([FromBody] EnrollmentRequest request)
        {
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (remoteIp is null)
            {
                return BadRequest();
            }

            _logger.LogTrace("Enrolling node with IP: {remoteIp}", remoteIp);
            if (!await _clusterService.OnEnrollAsync(request, remoteIp))
            {
                _logger.LogWarning("Failed to enroll node with IP: {remoteIp}", remoteIp);
                return BadRequest();
            }

            return Ok();
        }
    }
}
