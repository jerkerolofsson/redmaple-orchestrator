using MediatR;
using RedMaple.Orchestrator.Contracts.Deployments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Domain.Deployments
{
    public record class AppDeploymentReadyNotification(ApplicationDeployment Deployment) : INotification;

    /// <summary>
    /// Raised when the deployment is starting (up)
    /// </summary>
    /// <param name="Deployment"></param>
    public record class AppDeploymentStartingNotification(ApplicationDeployment Deployment) : INotification;

    /// <summary>
    /// Raised when the deployment is stopping (down)
    /// </summary>
    /// <param name="Deployment"></param>
    public record class AppDeploymentStoppingNotification(ApplicationDeployment Deployment) : INotification;
}
