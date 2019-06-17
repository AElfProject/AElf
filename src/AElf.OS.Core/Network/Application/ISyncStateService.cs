using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Node.Application;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Application
{
    public interface ISyncStateService
    {
        bool IsSyncFinished { get; }
        long CurrentSyncTarget { get; }
        Task UpdateSyncStateAsync();
    }

    public class SyncStateService : ISyncStateService, ISingletonDependency
    {
        private readonly INodeSyncStateProvider _syncStateProvider;
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockchainNodeContextService _blockchainNodeContextService;
        private readonly IPeerPool _peerPool;

        public ILogger<SyncStateService> Logger { get; set; }
        
        public SyncStateService(INodeSyncStateProvider syncStateProvider, IBlockchainService blockchainService, 
            IBlockchainNodeContextService blockchainNodeContextService, IPeerPool peerPool)
        {
            _syncStateProvider = syncStateProvider;
            _blockchainService = blockchainService;
            _blockchainNodeContextService = blockchainNodeContextService;
            _peerPool = peerPool;
        }
        
        public bool IsSyncFinished => _syncStateProvider.SyncTarget == -1;
        public long CurrentSyncTarget => _syncStateProvider.SyncTarget;
        private void SetSyncTarget(long value) => _syncStateProvider.SetSyncTarget(value);

        /// <summary>
        /// Updates the current target for the initial sync. For now this method will
        /// not have any effect if the sync is already finished.
        /// </summary>
        /// <returns></returns>
        public async Task UpdateSyncStateAsync()
        {
            if (IsSyncFinished)
                return;
                
            var chain = await _blockchainService.GetChainAsync();
            
            // if the current LIB is higher than the recorded target, update
            // the peers current LIB height. Note that this condition will 
            // also be true when the node starts.
            if (chain.LastIrreversibleBlockHeight >= _syncStateProvider.SyncTarget)
            {
                // Update handshake information of all our peers
                var tasks = _peerPool.GetPeers().Select(async peer =>
                {
                    try
                    {
                        await peer.UpdateHandshakeAsync();
                    }
                    catch (NetworkException e)
                    {
                        Logger.LogError(e, "Error while updating the lib.");
                    }
                }).ToList();
                
                await Task.WhenAll(tasks);
                await TryFindSyncTargetAsync();
            }
        }

        /// <summary>
        /// Based on the given list of peer, will update the target.
        /// </summary>
        /// <returns></returns>
        private async Task TryFindSyncTargetAsync()
        {
            // set the target to the lowest LIB
            var chain = await _blockchainService.GetChainAsync();
            var peers = _peerPool.GetPeers().ToList();
            
            long minSyncTarget = chain.LastIrreversibleBlockHeight + NetworkConstants.DefaultInitialSyncOffset;
            
            // determine the peers that are high enough to sync to
            var candidates = peers
                .Where(p => p.LastKnowLibHeight >= minSyncTarget)
                .OrderBy(p => p.LastKnowLibHeight)
                .ToList();

            if (candidates.Count == 0)
            {
                // no peer has a LIB to sync to, stop the sync.
                SetSyncAsFinished();
                Logger.LogDebug("Finishing sync, no peer has as a LIB.");
            }
            else
            {
                // If there's more than 2/3 of the nodes that we can 
                // sync to, take the lowest of them as target.
                var minLib = candidates.First().LastKnowLibHeight;
                
                if (candidates.Count >= Math.Ceiling(2d/3 * peers.Count))
                {
                    SetSyncTarget(minLib);
                    Logger.LogDebug($"Set sync target to {minLib}.");
                }
                else
                {
                    SetSyncAsFinished();
                    Logger.LogDebug("Finishing sync, no peer has as a LIB high enough.");
                }
            }
        }
        
        /// <summary>
        /// Finalizes the sync by changing the target to -1 and launching the
        /// notifying the Kernel of this change.
        /// </summary>
        private void SetSyncAsFinished()
        {
            _syncStateProvider.SetSyncTarget(-1);
            _blockchainNodeContextService.FinishSync();
        }
    }
}