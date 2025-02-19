﻿using Docker.DotNet.Models;
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
        /// Saves a deployment plan
        /// </summary>
        /// <param name="plan"></param>
        /// <returns></returns>
        Task SaveAsync(DeploymentPlan plan);
        Task<ValidationResult> ValidatePlanAsync(DeploymentPlan plan, bool validateDomainName = true);

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
        Task<List<Deployment>> GetApplicationDeploymentsAsync(string slug);
        Task<List<DeployedContainer>> GetDeployedContainersAsync(Deployment appDeployment);
        Task WaitUntilReadyzAsync(DeploymentPlan plan, Deployment applicationDeployment, IProgress<string> progress, CancellationToken cancellationToken);
        string CreateSlug(string deploymentName);
        List<VolumeBind> GetMissingVolumeNames(DeploymentPlan plan);
        Task RestartAsync(DeploymentPlan plan, IProgress<string> progress, CancellationToken cancellationToken);
        Task ChangeImageTagAsync(DeploymentPlan plan, string tag);
    }
}
