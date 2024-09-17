using CliWrap;
using RedMaple.Orchestrator.Contracts.Deployments;
using RedMaple.Orchestrator.DockerCompose;
using RedMaple.Orchestrator.Node.Controllers;
using System.Runtime.InteropServices;
using System.Text;

namespace RedMaple.Orchestrator.Node.LocalDeployments
{
    public class LocalDeploymentService : ILocalDeploymentService
    {
        private readonly ILogger<LocalDeploymentService> _logger;
        private readonly IDockerCompose _compose;
        public LocalDeploymentService(ILogger<LocalDeploymentService> logger, IDockerCompose compose)
        {
            _logger = logger;
            _compose = compose;
        }

        private string GetLocalConfigDir(string deployment)
        {
            // Note: It is important that the folder name is the slug,
            // as that will be the project name set for docker compose,
            // which we need to find the containers when
            // working with the deployments
            string path = $"/data/redmaple/node/deployments/{deployment}";
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (isWindows)
            {
                path = @$"C:\temp\redmaple\node\deployments\{deployment}";
            }
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }
        public async Task AddDeploymentAsync(AddNodeDeploymentRequest plan, IProgress<string> progress)
        {
            // Validate it

            // Generate up/down scripts
            await SaveHttpsCertificateAsync(plan, progress);
            await GenerateScriptsAsync(plan, progress);
        }

        private async Task SaveHttpsCertificateAsync(AddNodeDeploymentRequest plan, IProgress<string> progress)
        {
            progress.Report("Writing certificates..");
            var dir = GetLocalConfigDir(plan.Slug);
            var path = Path.Combine(dir, "https.pfx");
            await File.WriteAllBytesAsync(path, plan.HttpsCertificatePfx);

            // Change the environment variable
            plan.EnvironmentVariables["REDMAPLE_APP_HTTPS_CERTIFICATE_HOST_PATH"] = path;
        }

        private async Task GenerateScriptsAsync(AddNodeDeploymentRequest plan, IProgress<string> progress)
        {
            progress.Report("Writing deployment configuration..");
            var dir = GetLocalConfigDir(plan.Slug);
            var envFile = Path.Combine(dir, "plan.env");
            var upFile = Path.Combine(dir, "up.sh");
            var downFile = Path.Combine(dir, "down.sh");

            switch(plan.Kind)
            {
                case DeploymentKind.DockerCompose:
                    progress.Report("Writing docker-compose.yaml..");
                    var dockerComposeFile = Path.Combine(dir, "docker-compose.yaml");
                    await WritePlanFileAsync(dockerComposeFile, plan.Plan);

                    break;
                default:
                    progress.Report("Unsupported plan kind " + plan.Kind);
                    throw new ArgumentException("Unsupported plan kind " + plan.Kind);
            }

            await WriteEnvironmentFileAsync(envFile, plan.EnvironmentVariables);
            await WriteUpScriptAsync(upFile, envFile);
            await WriteDownScriptAsync(downFile, envFile);
        }

        private async Task WriteDownScriptAsync(string path, string envFile)
        {
            StringBuilder contents = new StringBuilder();
            contents.AppendLine("#!/usr/bin/bash");
            contents.AppendLine("");
            contents.AppendLine($"docker compose --env-file plan.env down");
            await File.WriteAllTextAsync(path, contents.ToString());
        }
        private async Task WriteUpScriptAsync(string path, string envFile)
        {
            StringBuilder contents = new StringBuilder();
            contents.AppendLine("#!/usr/bin/bash");
            contents.AppendLine("");
            contents.AppendLine($"docker compose --env-file plan.env up -d");
            await File.WriteAllTextAsync(path, contents.ToString());
        }
        private async Task WritePlanFileAsync(string path, string contents)
        {
            await File.WriteAllTextAsync(path, contents);
        }
        private async Task WriteEnvironmentFileAsync(string envFile, Dictionary<string, string> environmentVariables)
        {
            StringBuilder contents = new StringBuilder();
            foreach(var env in environmentVariables)
            {
                contents.AppendLine(env.Key + "=" + env.Value);
            }
            await File.WriteAllTextAsync(envFile, contents.ToString());
        }
        public async Task DeleteDeploymentAsync(string slug, IProgress<string> progress, CancellationToken cancellationToken)
        {
            try
            {
                await DownAsync(slug, progress, cancellationToken);
            }
            catch(Exception ex)
            {
                progress.Report($"Failed to stop deployment: {slug}: {ex.Message}");
            }
            var dir = GetLocalConfigDir(slug);
            try
            {
                Directory.Delete(dir, true);
            }
            catch
            {
                progress.Report($"Failed to delete directory: {dir}");
            }
        }

        private string GetDockerExePath()
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (isWindows)
            {
                return "docker";
            }
            return "/usr/bin/docker";
        }

        public async Task DownAsync(string slug, IProgress<string> progress, CancellationToken cancellationToken)
        {
            var dir = GetLocalConfigDir(slug);
            var composePath = Path.Combine(dir, "docker-compose.yaml");
            var envPath = Path.Combine(dir, "plan.env");

            var plan = DockerComposeParser.ParseFile(composePath);

            await _compose.DownAsync(progress, plan, envPath, cancellationToken);
        }

        public async Task UpAsync(string slug, IProgress<string> progress, CancellationToken cancellationToken)
        {
            var dir = GetLocalConfigDir(slug);
            var composePath = Path.Combine(dir, "docker-compose.yaml");
            var envPath = Path.Combine(dir, "plan.env");

            var plan = DockerComposeParser.ParseFile(composePath);
            await _compose.UpAsync(progress, plan, envPath, cancellationToken);
        }

        private async Task DownCliAsync(string slug, IProgress<string> progress, CancellationToken cancellationToken)
        { 
            var dir = GetLocalConfigDir(slug);

            _logger.LogInformation("Running docker compose down for path: {path}", dir);

            var task = Cli.Wrap(GetDockerExePath())
                .WithArguments(args =>
                {
                    args.Add("compose");
                    args.Add("--env-file");
                    args.Add("plan.env");
                    args.Add("down");
                })
                .WithWorkingDirectory(dir)
                .WithStandardErrorPipe(PipeTarget.ToDelegate((line) =>
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        progress.Report(line);
                    }
                }))
                .WithStandardOutputPipe(PipeTarget.ToDelegate((line) =>
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        progress.Report(line);
                    }
                })).ExecuteAsync(cancellationToken);
            await task.Task;
        }

        public async Task UpCliAsync(string slug, IProgress<string> progress, CancellationToken cancellationToken)
        {
            var dir = GetLocalConfigDir(slug);
            _logger.LogInformation("Running docker compose up for path: {path}", dir);

            var task = Cli.Wrap(GetDockerExePath())
                .WithArguments(args =>
                {
                    args.Add("compose");
                    args.Add("--env-file");
                    args.Add("plan.env");
                    args.Add("up");
                    args.Add("-d");
                })
                .WithWorkingDirectory(dir)
                .WithStandardErrorPipe(PipeTarget.ToDelegate((line) =>
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        progress.Report(line);
                    }
                }))
                .WithStandardOutputPipe(PipeTarget.ToDelegate((line) =>
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        progress.Report(line);
                    }
                })).ExecuteAsync(cancellationToken);
            await task.Task;
        }
    }
}
