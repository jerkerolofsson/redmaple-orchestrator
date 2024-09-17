using MediatR;
using RedMaple.Orchestrator.Contracts.Deployments;
using RedMaple.Orchestrator.Contracts.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Domain.Cluster
{
    public record class NodeKeepAliveReceivedNotification(NodeInfo Node) : INotification;
    public record class NodeEnrolledNotification(NodeInfo Node) : INotification;
}
