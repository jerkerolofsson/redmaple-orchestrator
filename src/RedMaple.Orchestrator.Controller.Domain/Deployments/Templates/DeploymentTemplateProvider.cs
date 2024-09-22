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

        public async Task DeleteTemplateAsync(string name)
        {
            await _repo.DeleteTemplateAsync(name);
        }

        public async Task<DeploymentPlanTemplate?> GetTemplateByNameAsync(string name)
        {
            return await _repo.GetTemplateByNameAsync(name);
        }

        public async Task<List<DeploymentPlanTemplate>> GetTemplatesAsync()
        {
            return await _repo.GetTemplatesAsync();
        }

        public async Task SaveTemplateAsync(DeploymentPlanTemplate template)
        {
            await _repo.AddTemplateAsync(template);
        }
    }
}
