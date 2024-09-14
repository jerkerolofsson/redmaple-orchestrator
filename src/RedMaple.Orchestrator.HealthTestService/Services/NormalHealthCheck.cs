using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RedMaple.Orchestrator.HealthTestService.Services
{
    public class NormalHealthCheck : IHealthCheck
    {
        private readonly NormalHealthCheckService _service;

        public NormalHealthCheck(NormalHealthCheckService service)
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
