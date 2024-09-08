using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts.Ingress
{
    public interface IIngressRepository
    {

        Task<List<IngressServiceDescription>> GetServicesAsync();
        Task DeleteIngressServiceAsync(string id);
        Task AddIngressServiceAsync(IngressServiceDescription service);
    }
}
