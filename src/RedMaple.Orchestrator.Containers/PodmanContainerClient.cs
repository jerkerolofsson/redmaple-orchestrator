using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Extensions.Logging;

namespace RedMaple.Orchestrator.Containers
{
    /// <summary>
    /// Podman implementation of the container client
    /// </summary>
    public class PodmanContainerClient : LocalContainersClient
    {
        public PodmanContainerClient(ILogger<LocalContainersClient> logger) : base(logger)
        {
        }

        protected override string GetDockerApiUri()
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

            string? dockerHost = Environment.GetEnvironmentVariable("DOCKER_HOST");
            if(dockerHost is not null)
            {
                return dockerHost;
            }

            if (isWindows)
            {
                return "npipe://./pipe/podman-machine-default";
            }
            if (isLinux)
            {
                string? uid = Environment.GetEnvironmentVariable("UID");
                if (uid is not null)
                {
                    return $"unix:///run/user/{uid}/podman/podman.sock";
                }
                return "unix:///run/docker.sock";
            }

            throw new Exception("Default socket location was not found. Set DOCKER_HOST environment variable to configure it!");
        }

        public override async Task LoginAsync(string registry, string username, string password, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Logging in to registry {Registry} using Podman", registry);

            var psi = new ProcessStartInfo
            {
                FileName = "podman",
                Arguments = $"login -u {EscapeArgument(username)} -p {EscapeArgument(password)} {EscapeArgument(registry)}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start podman login process");
            }

            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                _logger.LogError("Podman login failed: {Error}", stderr);
                throw new InvalidOperationException($"Podman login failed: {stderr}");
            }

            _logger.LogInformation("Successfully logged in to registry {Registry} using Podman", registry);
        }
    }
}
