
using RedMaple.Orchestrator.Contracts;
using System.Text;
using System.Net.Http.Json;

namespace RedMaple.Orchestrator.Node.Enrollment
{
    public class EnrollAtStartup : BackgroundService
    {
        private readonly HttpClient _httpClient;
        private ILogger<EnrollAtStartup> _logger;

        public EnrollAtStartup(HttpClient httpClient, ILogger<EnrollAtStartup> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(5));

            while (!stoppingToken.IsCancellationRequested)
            {
                // Connect to controller
                try
                {
                    var nodeInfo = new EnrollmentRequest()
                    {
                        Id = Guid.NewGuid().ToString(),
                    };

                    using var response = await _httpClient.PostAsJsonAsync("http://controller/api/nodes", nodeInfo);
                    response.EnsureSuccessStatusCode();
                    _logger.LogInformation("Successfully enrolled with controller");
                    return;
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex,"Failed to enroll with controller");
                }
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }
}
