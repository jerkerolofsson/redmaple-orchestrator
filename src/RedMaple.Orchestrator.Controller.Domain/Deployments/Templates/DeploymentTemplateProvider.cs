using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RedMaple.Orchestrator.Contracts.Deployments;

namespace RedMaple.Orchestrator.Controller.Domain.Deployments.Templates
{
    internal class DeploymentTemplateProvider : IDeploymentTemplateProvider
    {
        private readonly IDeploymentPlanTemplateRepository _repo;

        public DeploymentTemplateProvider(IDeploymentPlanTemplateRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<DeploymentPlanTemplate>> GetDeploymentsAsync()
        {
            return await _repo.GetDeploymentPlansAsync();
        }
    }
}
