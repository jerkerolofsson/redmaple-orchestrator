﻿using RedMaple.Orchestrator.Contracts.Ingress;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Domain.Ingress
{
    public interface IIngressManager : IIngressServiceController
    {
        Task<IngressServiceDescription?> GetIngressServiceByDomainNameAsync(string domainName, string? region);
        Task<bool> TryDeleteIngressServiceByDomainNameAsync(string? domainName, string? region);
    }
}
