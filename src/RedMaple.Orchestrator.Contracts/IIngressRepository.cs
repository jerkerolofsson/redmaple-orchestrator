using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Contracts
{
    public interface IIngressRepository
    {
        IQueryable<IngressServiceDescription> Services { get; }

        Task AddIngressServiceAsync(IngressServiceDescription service);
    }
}
