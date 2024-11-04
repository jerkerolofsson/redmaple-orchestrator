
using RedMaple.Orchestrator.Contracts.Ingress;
using RedMaple.Orchestrator.Ingress;
using static Grpc.Core.Metadata;

namespace RedMaple.Orchestrator.Node.Controllers
{
    [ApiController]
    [Route("/api/ingress")]
    public class IngressController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly LocalIngressServiceController _ingress;

        public IngressController(ILogger<IngressController> logger, LocalIngressServiceController ingress)
        {
            _logger = logger;
            _ingress = ingress;
        }

        [HttpGet("/api/ingress")]
        public async Task<List<IngressServiceDescription>> GetServicesAsync()
        {
            return await _ingress.GetServicesAsync();
        }

        [HttpPost("/api/ingress")]
        public async Task<string> AddIngressServiceAsync([FromBody] IngressServiceDescription service, CancellationToken cancellationToken)
        {
            var results = new ProgressResultBuilder();
            await _ingress.AddIngressServiceAsync(service, results, cancellationToken);
            return results.ToString() ?? "";
        }

        [HttpDelete("/api/ingress/{id}")]
        public async Task DeleteIngressServiceAsync([FromRoute] string id, CancellationToken cancellationToken)
        {
            await _ingress.DeleteIngressServiceAsync(id, cancellationToken);
        }

    }
}
