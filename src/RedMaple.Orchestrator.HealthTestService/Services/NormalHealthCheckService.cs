﻿using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RedMaple.Orchestrator.HealthTestService.Services
{
    public class NormalHealthCheckService
    {
        public HealthStatus Status { get; set; } = HealthStatus.Healthy;
        public string Description { get; set; } = "A healthy result";

    }
}