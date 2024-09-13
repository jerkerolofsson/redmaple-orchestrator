using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RedMaple.Orchestrator.HealthTestService.Services
{
    public class LiveHealthCheck : IHealthCheck
    {
        private readonly LiveHealthCheckService _service;

        public LiveHealthCheck(LiveHealthCheckService service)
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
