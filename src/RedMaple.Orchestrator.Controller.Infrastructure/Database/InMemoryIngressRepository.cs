using RedMaple.Orchestrator.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Infrastructure.Database
{
    internal class InMemoryIngressRepository : IIngressRepository
    {
        private List<IngressServiceDescription> _services = new();

        public IQueryable<IngressServiceDescription> Services => _services.AsQueryable();

        public Task AddIngressServiceAsync(IngressServiceDescription nodeInfo)
        {
            _services.Add(nodeInfo);
            return Task.CompletedTask;
        }
    }
}
