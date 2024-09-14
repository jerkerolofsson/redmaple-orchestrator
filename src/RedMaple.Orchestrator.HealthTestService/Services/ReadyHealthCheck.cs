using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RedMaple.Orchestrator.HealthTestService.Services
{
    public class ReadyHealthCheck : IHealthCheck
    {
        private readonly ReadyHealthCheckService _service;

        public ReadyHealthCheck(ReadyHealthCheckService service)
        {
            _service = service;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, 
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new HealthCheckResult(_service.Status, _service.Description));
        }
    }
}
