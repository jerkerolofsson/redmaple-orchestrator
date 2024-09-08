using Microsoft.AspNetCore.Mvc;
using RedMaple.Orchestrator.Contracts.Node;
using RedMaple.Orchestrator.Controller.Domain;
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

        public NodeController(
            ILogger<NodeController> logger,
            INodeManager repository)
        {
            _logger = logger;
            _repository = repository;
        }

        [HttpPost]
        public async Task<ActionResult> EnrollAsync([FromBody] EnrollmentRequest request)
        {
            _logger.LogInformation("Enrolling node...");
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (remoteIp is null)
            {
                return BadRequest();
            }

            _logger.LogInformation("Enrolling node, remoteip={IP}", remoteIp);
            string[] addresses = [remoteIp, ..request.HostAddresses];

            foreach (var addr in addresses)
            {
                // See where we can connect back
                if (IPAddress.TryParse(addr, out var ipAddress))
                {
                    if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                        !addr.StartsWith("::1"))
                    {
                        try
                        {
                            var url = $"{request.Schema}://{ipAddress}:{request.Port}";
                            _logger.LogInformation("Testing connection to {url}", url);
                            using var client = new NodeContainersClient(url);
                            var _ = await client.GetContainersAsync();
                            remoteIp = ipAddress.ToString();
                            break;
                        }
                        catch(Exception ex)
                        {
                            _logger.LogError(ex, "Failed to access node on that IP");
                        }
                    }
                }
            }

            _logger.LogInformation("Enrolling node IP={IP}, Port={Port}, Schema={Schema}", remoteIp, request.Port, request.Schema);
            var nodeInfo = new NodeInfo 
            { 
                Id = request.Id, 
                HostAddresses = request.HostAddresses, 
                IpAddress = remoteIp,
                Port = request.Port,
                Schema = request.Schema,
            };
            await _repository.EnrollNodeAsync(nodeInfo);
            return Ok();
        }
    }
}
