using Docker.DotNet.Models;
using FluentValidation.Results;
using RedMaple.Orchestrator.Contracts.Containers;
using RedMaple.Orchestrator.Contracts.Deployments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Domain.Deployments
{
    public interface IDeploymentManager
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
        Task PullImagesAsync(DeploymentPlan plan, IProgress<string> progress, IProgress<JSONMessage> pullProgress, CancellationToken cancellationToken);
        Task BringUpAsync(DeploymentPlan plan, IProgress<string> progress, CancellationToken cancellationToken);
        Task TakeDownAsync(DeploymentPlan plan, IProgress<string> progress, CancellationToken cancellationToken);
        Task DeleteAsync(DeploymentPlan plan, IProgress<string> progress, CancellationToken cancellationToken);
        Task<DeploymentPlan?> GetDeploymentBySlugAsync(string slug);

        /// <summary>
        /// Returns all deployed containers for a specific deployment plan as identified
        /// by the slug
        /// </summary>
        /// <param name="slug"></param>
        /// <returns></returns>
        Task<List<DeployedContainer>> GetDeployedContainersAsync(string slug);

        /// <summary>
        /// Returns all deployed applications
        /// </summary>
        /// <param name="slug"></param>
        /// <returns></returns>
        Task<List<ApplicationDeployment>> GetApplicationDeploymentsAsync(string slug);
        Task<List<DeployedContainer>> GetDeployedContainersAsync(ApplicationDeployment appDeployment);
        Task WaitUntilReadyzAsync(DeploymentPlan plan, ApplicationDeployment applicationDeployment, IProgress<string> progress, CancellationToken cancellationToken);
    }
}
