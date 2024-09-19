using MediatR;
using Microsoft.Extensions.Logging;
using RedMaple.Orchestrator.Contracts.Node;
using RedMaple.Orchestrator.Controller.Domain.Node;
using RedMaple.Orchestrator.Sdk;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Controller.Domain.Cluster
{
    internal class ClusterService : IClusterService
    {
        private readonly ILogger<ClusterService> _logger;
        private readonly INodeManager _repository;
        private readonly IMediator _mediator;
        private ConcurrentDictionary<string, NodeInfo> _nodes = new ConcurrentDictionary<string, NodeInfo>();

        public ClusterService(
            ILogger<ClusterService> logger,
            IMediator mediator,
            INodeManager repository)
        {
            _logger = logger;
            _mediator = mediator;
            _repository = repository;
        }

        public async Task<bool> OnEnrollAsync(EnrollmentRequest request, string remoteIp)
        {
            _logger.LogInformation("Enrolling node, remoteip={IP}", remoteIp);
            string[] addresses = [.. request.HostAddresses, remoteIp];

            bool success = false;
            foreach (var addr in addresses)
            {
                // See where we can connect back
                if (IPAddress.TryParse(addr, out var ipAddress))
                {
                    if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                        !addr.StartsWith("::1"))
                    {
                        try
                        {
                            var url = $"{request.Schema}://{ipAddress}:{request.Port}";
                            _logger.LogInformation("Testing node connection to {url}", url);
                            using var client = new NodeContainersClient(url);
                            var _ = await client.GetContainersAsync();
                            remoteIp = ipAddress.ToString();
                            success = true;
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to access node on that IP");
                        }
                    }
                }
            }

            if (!success)
            {
                return false;
            }

            var nodeInfo = new NodeInfo
            {
                Id = request.Id,
                HostAddresses = request.HostAddresses,
                IpAddress = remoteIp,
                Port = request.Port,
                Schema = request.Schema,
                IngressHttpsPort = request.IngressHttpsPort,
                Tags = request.Tags.Split(' ').ToList(),
                IsDnsEnabled = request.IsDnsEnabled,
                IsIngressEnabled = request.IsIngressEnabled,
                IsLoadBalancerEnabled = request.IsLoadBalancerEnabled,
                IsApplicationHostEnabled = request.IsApplicationHostEnabled,
                Name = request.Name,
                Region = request.Region
            };
            await _repository.EnrollNodeAsync(nodeInfo);

            if(_nodes.ContainsKey(nodeInfo.IpAddress))
            {
                _logger.LogDebug("Keep-Alive received from node IP={IP}, Port={Port}, Schema={Schema}", remoteIp, request.Port, request.Schema);
                await _mediator.Publish(new NodeKeepAliveReceivedNotification(nodeInfo));
            }
            else
            {
                _logger.LogInformation("Enrolled node IP={IP}, Port={Port}, Schema={Schema}", remoteIp, request.Port, request.Schema);
                _nodes[nodeInfo.IpAddress] = nodeInfo;
                await _mediator.Publish(new NodeEnrolledNotification(nodeInfo));
            }

            return true;
        }
    }
}
