using Docker.DotNet.Models;
using RedMaple.Orchestrator.Contracts.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Containers
{
    internal class ContainerMapping
    {
        internal static Container Map(ContainerListResponse container)
        {
            var name = container.Names.FirstOrDefault();
            if (name is not null)
            {
                name = name.TrimStart('/');
            }
            return new Container
            {
                Id = container.ID,
                Name = name,
                Status = container.Status,
                State = container.State,
                IsRunning = container.State == "running",
                Labels = new Dictionary<string,string>(container.Labels),
                Image = container.Image,
                Ports = new List<ContainerPort>(container.Ports.Select(x => new ContainerPort
                {
                    PrivatePort = x.PrivatePort,
                    PublicPort = x.PublicPort,
                    Ip = x.IP,
                    Type = x.Type
                }))
            };
        }
    }
}
