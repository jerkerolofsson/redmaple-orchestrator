using RedMaple.Orchestrator.Contracts.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.DockerCompose
{
    public partial class DockerCompose
    {
        public async Task<Container?> FindContainerAsync(DockerComposePlan plan, string containerName, CancellationToken cancellationToken)
        {
            var containers = await _docker.GetContainersAsync(plan.ProjectName, cancellationToken);
            foreach (var container in containers)
            {
                if(container.Name == containerName)
                {
                    return container;
                }
            }
            return null;
        }
    }
}
