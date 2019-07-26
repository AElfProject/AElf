using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Node.Application;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Application
{
    public interface ISyncStateService
    {
        SyncState SyncState { get; }
        long GetCurrentSyncTarget();
        Task StartSyncAsync();
        Task UpdateSyncStateAsync();
    }

    public enum SyncState { UnInitialized, Syncing, Finished }

    public class SyncStateService : ISyncStateService, ISingletonDependency
    {
        private NetworkOptions NetworkOptions => NetworkOptionsSnapshot.Value;
        public IOptionsSnapshot<NetworkOptions> NetworkOptionsSnapshot { get; set; }
        
        private readonly INodeSyncStateProvider _syncStateProvider;
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockchainNodeContextService _blockchainNodeContextService;
        private readonly IPeerPool _peerPool;
        private readonly IHandshakeProvider _handshakeProvider;

        public ILogger<SyncStateService> Logger { get; set; }
        
        public SyncStateService(INodeSyncStateProvider syncStateProvider, IBlockchainService blockchainService, 
            IBlockchainNodeContextService blockchainNodeContextService, IPeerPool peerPool, 
            IHandshakeProvider handshakeProvider)
        {
            _syncStateProvider = syncStateProvider;
            _blockchainService = blockchainService;
            _blockchainNodeContextService = blockchainNodeContextService;
            _peerPool = peerPool;
            _handshakeProvider = handshakeProvider;
        }
        
        public long GetCurrentSyncTarget() => _syncStateProvider.SyncTarget;

        public SyncState SyncState
        {
            get
            {
                switch (_syncStateProvider.SyncTarget)
                {
                    case 0:
                        return SyncState.UnInitialized;
                    case -1:
                        return SyncState.Finished;
                    default:
                        return SyncState.Syncing;
                }
            }
        }
        
        private void SetSyncTarget(long value) => _syncStateProvider.SetSyncTarget(value);

        /// <summary>
        /// Based on current peers, will determine if a sync is needed or not. This method
        /// should only be called once, to go from an uninitialized state to either syncing
        /// or not syncing.
        /// </summary>
        /// <returns></returns>
        public async Task StartSyncAsync()
        {
            if (SyncState != SyncState.UnInitialized)
            {
                Logger.LogWarning("Trying to start the sync, but it has already been started/finished.");
                return;
            }

            await UpdateSyncTargetAsync();
        }

        /// <summary>
        /// Updates the current target for the initial sync. For now this method will
        /// not have any effect if the sync is already finished or the target has not
        /// been initialized.
        /// </summary>
        /// <returns></returns>
        public async Task UpdateSyncStateAsync()
        {
            // This method should only be called when the sync target has already been found and the
            // node is syncing.
            
            if (SyncState != SyncState.Syncing)
            {
                Logger.LogWarning("Trying to update the sync, but it is either finished or not yet been initialized.");
                return;
            }
                
            var chain = await _blockchainService.GetChainAsync();
            
            // if the current LIB is higher than the recorded target, update
            // the peers current LIB height. Note that this condition will 
            // also be true when the node starts.
            if (chain.LastIrreversibleBlockHeight >= _syncStateProvider.SyncTarget)
            {
                var handshake = await _handshakeProvider.GetHandshakeAsync();
                
                // Update handshake information of all our peers
                var tasks = _peerPool.GetPeers().Select(async peer =>
                {
                    try
                    {
                        await peer.DoHandshakeAsync(handshake);
                    }
                    catch (NetworkException e)
                    {
                        Logger.LogError(e, "Error while handshaking.");
                    }
                    
                    Logger.LogDebug($"Peer {peer} last known LIB is {peer.LastKnownLibHeight}.");
                    
                }).ToList();
                
                await Task.WhenAll(tasks);
                await UpdateSyncTargetAsync();
            }
        }

        /// <summary>
        /// Based on the given list of peer, will update the sync target. It take the peers that have an LIB higher 
        /// than (our LIB + offset), these constitute the possible nodes to sync to. If this group constitutes at
        /// least ceil(2/3 * peer_count), take the one with the smallest LIB as target. Like this:
        /// peer count: 1, nodes that must be higher: 1 - Note: if only one peer, sync.
        /// peer count: 2, nodes that must be higher: 2
        /// peer count: 3, nodes that must be higher: 2
        /// peer count: 4, nodes that must be higher: 3
        /// </summary>
        /// <returns></returns>
        private async Task UpdateSyncTargetAsync()
        {
            // set the target to the lowest LIB
            var chain = await _blockchainService.GetChainAsync();
            var peers = _peerPool.GetPeers().ToList();
            
            long minSyncTarget = chain.LastIrreversibleBlockHeight + NetworkOptions.InitialSyncOffset;
            
            // determine the peers that are high enough to sync to
            var candidates = peers
                .Where(p => p.LastKnownLibHeight >= minSyncTarget)
                .OrderBy(p => p.LastKnownLibHeight)
                .ToList();

            if (candidates.Count == 0)
            {
                // no peer has a LIB to sync to, stop the sync.
                await SetSyncAsFinishedAsync();
                Logger.LogDebug($"Finishing sync, not enough peers have a sufficiently high LIB (peer count: {_peerPool.PeerCount}).");
            }
            else
            {
                // If there's more than 2/3 of the nodes that we can 
                // sync to, take the lowest of them as target.
                var minLib = candidates.First().LastKnownLibHeight;
                
                if (candidates.Count >= Math.Ceiling(2d/3 * peers.Count))
                {
                    SetSyncTarget(minLib);
                    Logger.LogDebug($"Set sync target to {minLib}.");
                }
                else
                {
                    await SetSyncAsFinishedAsync();
                    Logger.LogDebug("Finishing sync, no peer has as a LIB high enough.");
                }
            }
        }
        
        /// <summary>
        /// Finalizes the sync by changing the target to -1 and launching the
        /// notifying the Kernel of this change.
        /// </summary>
        private async Task SetSyncAsFinishedAsync()
        {
            SetSyncTarget(-1);
            await _blockchainNodeContextService.FinishInitialSyncAsync();
        }
    }
}