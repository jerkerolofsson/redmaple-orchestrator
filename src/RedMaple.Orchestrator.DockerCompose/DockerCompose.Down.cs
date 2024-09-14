using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.DockerCompose.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.DockerCompose
{
    public partial class DockerCompose
    {
        public async Task DownAsync(
            IProgress<string> progress,
            DockerComposePlan plan,
            string? environmentFile,
            CancellationToken cancellationToken)
        {
            plan = TransformPlanWithEnvironmentVariables(plan, environmentFile);

            // Find containers
            await DownContainersAsync(progress, plan, cancellationToken);

            progress.Report($"Deleting default docker network..");
            await DeleteDefaultNetworkAsync(plan, cancellationToken);
        }

        private async Task DownContainersAsync(IProgress<string> progress, DockerComposePlan plan, CancellationToken cancellationToken)
        {
            if (!plan.Labels.TryGetValue(DockerComposeConstants.LABEL_PROJECT, out var planProject) || string.IsNullOrWhiteSpace(planProject))
            {
                _logger.LogError("Internal Error: Failed to read project from compose file");
                return;
            }
            List<string> containerIds = new List<string>();
            foreach (var container in await _docker.GetContainersAsync(cancellationToken))
            {
                if (container.Labels is not null)
                {
                    if (container.Labels.TryGetValue(DockerComposeConstants.LABEL_PROJECT, out var containerProject))
                    {
                        if (containerProject == planProject)
                        {
                            containerIds.Add(container.Id);
                        }
                    }
                }
            }

            foreach (var containerId in containerIds)
            {
                progress.Report($"Stopping {containerId}..");
                await _docker.StopAsync(containerId, cancellationToken);
            }

            foreach (var containerId in containerIds)
            {
                progress.Report($"Deleting {containerId}..");
                await _docker.DeleteContainerAsync(containerId, cancellationToken);
            }
        }
    }
}
