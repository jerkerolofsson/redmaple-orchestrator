﻿using MediatR;
using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.Contracts.Node;
using RedMaple.Orchestrator.Controller.Domain.Node.Notifications;

namespace RedMaple.Orchestrator.Controller.Domain.Node
{
    public class NodeManager : INodeManager
    {
        private readonly INodeRepository _nodeRepository;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public NodeManager(
            ILogger<NodeManager> logger,
            IMediator mediator,
            INodeRepository nodeRepository)
        {
            _logger = logger;
            _mediator = mediator;
            _nodeRepository = nodeRepository;
        }

        public async Task EnrollNodeAsync(NodeInfo nodeInfo)
        {
            ArgumentNullException.ThrowIfNull(nodeInfo?.IpAddress);

            var existingNode = await GetNodeByIpAddressAsync(nodeInfo.IpAddress);
            if (existingNode is null)
            {
                _logger.LogInformation("Node enrolled: {BaseUrl}", nodeInfo.BaseUrl);
                await _nodeRepository.AddNodeAsync(nodeInfo);
                await _mediator.Publish(new NodeEnrolledNotification(nodeInfo));
            }
            else
            {
                _logger.LogInformation("Node already enrolled: {BaseUrl}", nodeInfo.BaseUrl);
            }
        }

        public async Task<NodeInfo?> GetNodeByIpAddressAsync(string ingressIp)
        {
            var nodes = await GetNodesAsync();
            foreach (var node in nodes)
            {
                if (node.IpAddress == ingressIp)
                {
                    return node;
                }
                if (node.HostAddresses is not null)
                {
                    foreach (var hostAddress in node.HostAddresses)
                    {
                        if (hostAddress == ingressIp)
                        {
                            return node;
                        }
                    }
                }
            }
            return null;
        }

        public Task<List<NodeInfo>> GetNodesAsync()
        {
            return Task.FromResult(_nodeRepository.Nodes.ToList());
        }
    }
}