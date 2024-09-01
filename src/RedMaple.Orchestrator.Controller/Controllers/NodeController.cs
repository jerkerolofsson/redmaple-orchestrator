using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RedMaple.Orchestrator.Containers;
using RedMaple.Orchestrator.Contracts;
using RedMaple.Orchestrator.Controller.Domain;
using System.Net;

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
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (remoteIp is null)
            {
                return BadRequest();
            }
            if (remoteIp == "::1")
            {
                remoteIp = "localhost";
                foreach (var addr in request.HostAddresses)
                {
                    // See where we can connect back
                    if (IPAddress.TryParse(addr, out var ipAddress))
                    {
                        if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                            !addr.StartsWith("127.0.0"))
                        {
                            try
                            {
                                using var client = new NodeContainersClient($"https://{ipAddress}:{request.Port}");
                                var _ = await client.GetContainersAsync();
                                remoteIp = ipAddress.ToString();
                                break;
                            }
                            catch(Exception)
                            {

                            }
                        }
                    }
                }
            }

            var nodeInfo = new NodeInfo { Id = request.Id, HostAddresses = request.HostAddresses };
            nodeInfo.IpAddress = remoteIp;

            await _repository.EnrollNodeAsync(nodeInfo);
            return Ok();
        }
    }
}
