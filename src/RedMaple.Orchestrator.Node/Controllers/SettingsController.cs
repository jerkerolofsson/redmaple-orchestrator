using Docker.DotNet.Models;
using Microsoft.AspNetCore.Mvc;
using RedMaple.Orchestrator.Contracts.Node;
using RedMaple.Orchestrator.Node.Settings;

namespace RedMaple.Orchestrator.Node.Controllers
{
    [ApiController]
    [Route("/api/settings")]
    public class SettingsController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly INodeSettingsProvider _settingsProvider;

        public SettingsController(ILogger<SettingsController> logger, INodeSettingsProvider settingsProvider)
        {
            _logger = logger;
            _settingsProvider = settingsProvider;
        }

        [HttpGet("/api/settings")]
        public async Task<NodeSettings> GetSettingsAsync()
        {
            return await _settingsProvider.GetSettingsAsync();
        }
        [HttpPost("/api/settings")]
        public async Task ApplySettingsAsync([FromBody] NodeSettings settings)
        {
            await _settingsProvider.ApplySettingsAsync(settings);
        }
    }
}
