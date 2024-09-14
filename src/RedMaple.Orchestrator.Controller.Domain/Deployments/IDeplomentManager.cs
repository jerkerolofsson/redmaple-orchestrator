using FluentValidation.Results;
using RedMaple.Orchestrator.Contracts.Deployments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Domain.Deployments
{
    public interface IDeplomentManager
    {
        /// <summary>
        /// Returns the default environment variables for a deployment
        /// </summary>
        /// <returns></returns>
        Dictionary<string, string> GetDefaultEnvironmentVariables(DeploymentPlan plan);

        /// <summary>
        /// Saves a deployment
        /// </summary>
        /// <param name="plan"></param>
        /// <returns></returns>
        Task SaveAsync(DeploymentPlan plan);
        ValidationResult ValidatePlan(DeploymentPlan plan);

        Task<List<DeploymentPlan>> GetDeploymentPlansAsync();
        Task BringUpAsync(DeploymentPlan plan, IProgress<string> progress, CancellationToken cancellationToken);
        Task TakeDownAsync(DeploymentPlan plan, IProgress<string> progress, CancellationToken cancellationToken);
        Task DeleteAsync(DeploymentPlan plan, IProgress<string> progress, CancellationToken cancellationToken);
    }
}
