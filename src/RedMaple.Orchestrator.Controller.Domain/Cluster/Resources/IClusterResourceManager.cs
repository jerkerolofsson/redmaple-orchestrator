﻿using RedMaple.Orchestrator.Contracts.Resources;

namespace RedMaple.Orchestrator.Controller.Domain.Cluster.Resources
{
    public interface IClusterResourceManager
    {
        Task SaveResourceAsync(ClusterResource resource);
        Task AddResourceAsync(ClusterResource resource);
        Task<ClusterResource?> GetClusterResourceAsync(string id);
        Task<List<ClusterResource>> GetClusterResourcesByPlanAsync(string planId);
        Task<List<ClusterResource>> GetClusterResourcesAsync();
        Task RemoveResourceAsync(ClusterResource resource);

        /// <summary>
        /// Finds cluster resources that provide missing environment variables
        /// </summary>
        /// <param name="environmentVariables"></param>
        /// <param name="region"></param>
        /// <returns></returns>
        Task<List<ClusterResource>> GetClusterResourcesAsync(Dictionary<string, string> environmentVariables, string? region);
    }
}