using MediatR;
using RedMaple.Orchestrator.Contracts.Node;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace RedMaple.Orchestrator.Node.Settings
{
    public class NodeSettingsProvider : INodeSettingsProvider
    {
        private NodeSettings? _settings;
        private readonly IMediator _mediator;

        public NodeSettingsProvider(IMediator mediator)
        {
            _mediator = mediator;
        }

        private string GetLocalConfigDir()
        {
            string path = "/etc/redmaple/node";
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (isWindows)
            {
                path = @"C:\temp\redmaple\node";
            }
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        private string GetConfigFilePath()
        {
            string path = GetLocalConfigDir();
            var filePath = Path.Combine(path, "node.json");

            if (!File.Exists(filePath))
            {
                File.Create(filePath).Close();
            }
            return filePath;
        }

        public async Task ApplySettingsAsync(NodeSettings settings)
        {
            _settings = settings;

            var json = JsonSerializer.Serialize(settings);
            var path = GetConfigFilePath();
            await File.WriteAllTextAsync(path, json);

            await _mediator.Publish(new NodeSettingsChangedNotification(settings));
        }

        public async Task<NodeSettings> GetSettingsAsync()
        {
            if(_settings is not null)
            {
                return _settings;
            }

            var path = GetConfigFilePath();
            try
            {
                var json = await File.ReadAllTextAsync(path);
                _settings = JsonSerializer.Deserialize<NodeSettings>(json);
            }
            catch (Exception) { }
            _settings ??= new();
            return _settings;
        }
    }
}
