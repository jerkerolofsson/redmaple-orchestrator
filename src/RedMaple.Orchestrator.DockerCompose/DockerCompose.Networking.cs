using Docker.DotNet.Models;
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
        private async Task<NetworkResponse?> CreateDefaultNetworkAsync(DockerComposePlan plan, CancellationToken cancellationToken)
        {
            var networks = await _docker.ListNetworksAsync(new NetworksListParameters(), cancellationToken);
            var defaultNetwork = networks.Where(x => x.Name == plan.name + "_default").FirstOrDefault();
            if (defaultNetwork is null)
            {
                await _docker.CreateNetworkAsync(new NetworksCreateParameters
                {
                    Name = plan.name + "_default",
                    Driver = "bridge"
                }, cancellationToken);
            }

            networks = await _docker.ListNetworksAsync(new NetworksListParameters(), cancellationToken);
            defaultNetwork = networks.Where(x => x.Name == plan.name + "_default").FirstOrDefault();
            return defaultNetwork;
        }

        private async Task DeleteDefaultNetworkAsync(DockerComposePlan plan, CancellationToken cancellationToken)
        {
            var networks = await _docker.ListNetworksAsync(new NetworksListParameters(), cancellationToken);
            var defaultNetwork = networks.Where(x => x.Name == plan.name + "_default").FirstOrDefault();
            if (defaultNetwork != null)
            {
                await _docker.DeleteNetworkAsync(defaultNetwork.ID, new ContainerRemoveParameters
                {
                    Force = true
                }, cancellationToken);
            }
        }
    }
}
