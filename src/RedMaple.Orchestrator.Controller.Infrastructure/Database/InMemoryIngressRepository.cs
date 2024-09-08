using RedMaple.Orchestrator.Contracts.Ingress;
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

        public InMemoryIngressRepository()
        {
        }

        public IQueryable<IngressServiceDescription> Services => _services.AsQueryable();

        public Task AddIngressServiceAsync(IngressServiceDescription service)
        {
            if(string.IsNullOrWhiteSpace(service.Id))
            {
                service.Id = Guid.NewGuid().ToString(); 
            }
            _services.Add(service);
            return Task.CompletedTask;
        }

        public Task<List<IngressServiceDescription>> GetServicesAsync()
        {
            List<IngressServiceDescription> services = _services.ToList();
            return Task.FromResult(services);
        }

        public Task DeleteIngressServiceAsync(string id)
        {
            _services.RemoveAll(x => x.Id == id);
            return Task.CompletedTask;
        }
    }
}
