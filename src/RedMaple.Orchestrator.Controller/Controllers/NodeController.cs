using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RedMaple.Orchestrator.Contracts;
using RedMaple.Orchestrator.Controller.Domain;

namespace RedMaple.Orchestrator.Controller.Controllers
{
    [Route("api/nodes")]
    [ApiController]
    public class NodeController : ControllerBase
    {
        private readonly INodeManager _repository;

        public NodeController(INodeManager repository)
        {
            _repository = repository;
        }

        [HttpPost]
        public async Task<ActionResult> EnrollAsync([FromBody] EnrollmentRequest request)
        {
            var nodeInfo = new NodeInfo { Id = request.Id };
            nodeInfo.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            if(nodeInfo.IpAddress is null)
            { 
                return BadRequest();
            }
            if(nodeInfo.IpAddress == "::1")
            {
                nodeInfo.IpAddress = "localhost";
            }

            await _repository.EnrollNodeAsync(nodeInfo);
            return Ok();
        }
    }
}
