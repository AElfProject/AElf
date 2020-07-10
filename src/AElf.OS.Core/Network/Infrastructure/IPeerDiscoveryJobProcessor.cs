using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using AElf.Kernel.Account.Application;
using AElf.OS.Network.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using AElf.OS.Network.Extensions;

namespace AElf.OS.Network.Infrastructure
{
    public interface IPeerDiscoveryJobProcessor
    {
        Task<bool> SendDiscoveryJobAsync(IPeer peer);
        Task CompleteAsync();
    }

    public class PeerDiscoveryJobProcessor : IPeerDiscoveryJobProcessor, ISingletonDependency
    {
        private readonly INodeManager _nodeManager;
        private readonly IDiscoveredNodeCacheProvider _discoveredNodeCacheProvider;
        private readonly IAElfNetworkServer _networkServer;
        private readonly IAccountService _accountService;

        private TransformManyBlock<IPeer, NodeInfo> _discoverNodesDataflow;
        private ActionBlock<NodeInfo> _processNodeDataflow;

        private const int DiscoverNodesBoundedCapacity = 50;
        private const int DiscoverNodesMaxDegreeOfParallelism = 5;
        private const int ProcessNodeBoundedCapacity = 200;
        private const int ProcessNodeMaxDegreeOfParallelism = 5;

        public ILogger<PeerDiscoveryJobProcessor> Logger { get; set; }

        public PeerDiscoveryJobProcessor(INodeManager nodeManager,
            IDiscoveredNodeCacheProvider discoveredNodeCacheProvider, IAElfNetworkServer networkServer,
            IAccountService accountService)
        {
            _nodeManager = nodeManager;
            _discoveredNodeCacheProvider = discoveredNodeCacheProvider;
            _networkServer = networkServer;
            _accountService = accountService;
            CreatePeerDiscoveryDataflow();

            Logger = NullLogger<PeerDiscoveryJobProcessor>.Instance;
        }

        public async Task<bool> SendDiscoveryJobAsync(IPeer peer)
        {
            return await _discoverNodesDataflow.SendAsync(peer);
        }

        public async Task CompleteAsync()
        {
            _discoverNodesDataflow.Complete();
            await _processNodeDataflow.Completion;
        }

        private void CreatePeerDiscoveryDataflow()
        {
            _discoverNodesDataflow = new TransformManyBlock<IPeer, NodeInfo>(
                async peer => await DiscoverNodesAsync(peer),
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = DiscoverNodesBoundedCapacity,
                    MaxDegreeOfParallelism = DiscoverNodesMaxDegreeOfParallelism
                });

            _processNodeDataflow =
                new ActionBlock<NodeInfo>(async node => await ProcessNodeAsync(node), new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = ProcessNodeBoundedCapacity,
                    MaxDegreeOfParallelism = ProcessNodeMaxDegreeOfParallelism
                });

            _discoverNodesDataflow.LinkTo(_processNodeDataflow, new DataflowLinkOptions {PropagateCompletion = true});
        }

        private async Task<List<NodeInfo>> DiscoverNodesAsync(IPeer peer)
        {
            try
            {
                var nodeList = await peer.GetNodesAsync();

                if (nodeList?.Nodes == null)
                    return new List<NodeInfo>();

                Logger.LogDebug($"Discover nodes: {nodeList} from peer: {peer}.");
                return nodeList.Nodes.ToList();
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, "Discover nodes failed.");
                return new List<NodeInfo>();
            }
        }

        private async Task ProcessNodeAsync(NodeInfo node)
        {
            try
            {
                if (!await ValidateNodeAsync(node))
                    return;

                if (await _nodeManager.AddNodeAsync(node))
                {
                    _discoveredNodeCacheProvider.Add(node.Endpoint);
                    Logger.LogDebug($"Discover and add node: {node.Endpoint} successfully.");
                }
                else
                {
                    var endpointLocal = await TakeEndpointFromDiscoveredNodeCacheAsync();
                    
                    if(endpointLocal.IsNullOrWhiteSpace())
                        return;
                    
                    if (await _networkServer.CheckEndpointAvailableAsync(endpointLocal))
                    {
                        _discoveredNodeCacheProvider.Add(endpointLocal);
                        Logger.LogDebug($"Only refresh node: {endpointLocal}.");
                    }
                    else
                    {
                        await _nodeManager.RemoveNodeAsync(endpointLocal);
                        if (await _nodeManager.AddNodeAsync(node))
                            _discoveredNodeCacheProvider.Add(node.Endpoint);

                        Logger.LogDebug(
                            $"Remove unavailable node: {endpointLocal}, and add node: {node.Endpoint} successfully.");
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, "Process node failed.");
            }
        }

        private async Task<bool> ValidateNodeAsync(NodeInfo node)
        {
            if ((await _accountService.GetPublicKeyAsync()).ToHex() == node.Pubkey.ToHex())
                return false;

            if (await _nodeManager.GetNodeAsync(node.Endpoint) != null)
            {
                await _nodeManager.UpdateNodeAsync(node);
                return false;
            }

            if (!await _networkServer.CheckEndpointAvailableAsync(node.Endpoint))
                return false;

            return true;
        }

        private async Task<string> TakeEndpointFromDiscoveredNodeCacheAsync()
        {
            while (_discoveredNodeCacheProvider.TryTake(out var endpoint))
            {
                if (await _nodeManager.GetNodeAsync(endpoint) != null)
                    return endpoint;
            }

            return null;
        }
    }
}