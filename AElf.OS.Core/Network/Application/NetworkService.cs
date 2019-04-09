using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Application
{
    public class NetworkService : INetworkService, ISingletonDependency
    {
        private readonly IPeerPool _peerPool;

        public ILogger<NetworkService> Logger { get; set; }

        public NetworkService(IPeerPool peerPool)
        {
            _peerPool = peerPool;

            Logger = NullLogger<NetworkService>.Instance;
        }

        public async Task<bool> AddPeerAsync(string address)
        {
            return await _peerPool.AddPeerAsync(address);
        }

        public async Task<bool> RemovePeerAsync(string address)
        {
            return await _peerPool.RemovePeerByAddressAsync(address);
        }

        public List<string> GetPeerIpList()
        {
            return _peerPool.GetPeers(true).Select(p => p.PeerIpAddress).ToList();
        }

        public async Task<int> BroadcastAnnounceAsync(BlockHeader blockHeader)
        {
            int successfulBcasts = 0;
            
            foreach (var peer in _peerPool.GetPeers())
            {
                try
                {
                    var announcement = new PeerNewBlockAnnouncement
                    {
                        BlockHash = blockHeader.GetHash(), BlockHeight = blockHeader.Height
                    };
                    Logger.LogDebug($"PeerNewBlockAnnouncement: {announcement}");
                    await peer.AnnounceAsync(announcement);

                    successfulBcasts++;
                }
                catch (NetworkException e)
                {
                    Logger.LogError(e, "Error while sending block.");
                }
            }

            return successfulBcasts;
        }

        public async Task<int> BroadcastTransactionAsync(Transaction tx)
        {
            int successfulBcasts = 0;
            
            foreach (var peer in _peerPool.GetPeers())
            {
                try
                {
                    await peer.SendTransactionAsync(tx);
                    successfulBcasts++;
                }
                catch (NetworkException e)
                {
                    Logger.LogError(e, "Error while sending transaction.");
                }
            }
            
            return successfulBcasts;
        }

        public async Task<List<Block>> GetBlocksAsync(Hash previousBlock, long previousHeight, int count, string peerPubKey = null, bool tryOthersIfFail = false)
        {
            // try get the block from the specified peer. 
            if (!string.IsNullOrWhiteSpace(peerPubKey))
            {
                IPeer peer = _peerPool.FindPeerByPublicKey(peerPubKey);

                if (peer == null)
                {
                    // if the peer was specified but we can't find it 
                    // we don't try any further.
                    Logger.LogWarning($"Specified peer was not found.");
                    return null;
                }

                var blocks = await RequestAsync(peer, p => p.GetBlocksAsync(previousBlock, count));

                if (blocks != null && blocks.Count > 0)
                    return blocks;

                if (!tryOthersIfFail)
                {
                    Logger.LogWarning($"{peerPubKey} does not have blocks {nameof(tryOthersIfFail)} is false.");
                    return null;
                }
            }
            
            // shuffle the peers that can give us the blocks
            var shuffledPeers = _peerPool.GetPeers()
                .Where(p => p.CurrentBlockHeight >= previousHeight)
                .OrderBy(a => Guid.NewGuid());
                
            foreach (var peer in shuffledPeers)
            {
                var blocks = await RequestAsync(peer, p => p.GetBlocksAsync(previousBlock, count));

                if (blocks != null)
                    return blocks;
            }

            return null;
        }

        public async Task<Block> GetBlockByHashAsync(Hash hash, string peer = null, bool tryOthersIfSpecifiedFails = false)
        {
            Logger.LogDebug($"Getting block by hash, hash: {hash} from {peer}.");
            return await GetBlockAsync(hash, peer, tryOthersIfSpecifiedFails);
        }

        private async Task<Block> GetBlockAsync(Hash hash, string peer = null, bool tryOthersIfSpecifiedFails = false)
        {
            if (tryOthersIfSpecifiedFails && string.IsNullOrWhiteSpace(peer))
                throw new InvalidOperationException($"Parameter {nameof(tryOthersIfSpecifiedFails)} cannot be true, " +
                                                    $"if no fallback peer is specified.");

            // try get the block from the specified peer. 
            if (!string.IsNullOrWhiteSpace(peer))
            {
                IPeer p = _peerPool.FindPeerByPublicKey(peer);

                if (p == null)
                {
                    // if the peer was specified but we can't find it 
                    // we don't try any further.
                    Logger.LogWarning($"Specified peer was not found.");
                    return null;
                }

                var block = await RequestBlockToAsync(hash, p);

                if (block != null)
                    return block;

                if (!tryOthersIfSpecifiedFails)
                {
                    Logger.LogWarning($"{peer} does not have block {nameof(tryOthersIfSpecifiedFails)} is false.");
                    return null;
                }
            }

            foreach (var p in _peerPool.GetPeers())
            {
                Block block = await RequestBlockToAsync(hash, p);

                if (block != null)
                    return block;
            }

            return null;
        }

        private async Task<Block> RequestBlockToAsync(Hash hash, IPeer peer)
        {
            return await RequestAsync(peer, p => p.RequestBlockAsync(hash));
        }

        private async Task<T> RequestAsync<T>(IPeer peer, Func<IPeer, Task<T>> func) where T : class
        {
            try
            {
                return await func(peer);
            }
            catch (NetworkException e)
            {
                Logger.LogError(e, $"Error while requesting block from {peer.PeerIpAddress}.");
                return null;
            }
        }

        public Task<long> GetBestChainHeightAsync(string peerPubKey = null)
        {
            var peer = !peerPubKey.IsNullOrEmpty()
                ? _peerPool.FindPeerByPublicKey(peerPubKey)
                : _peerPool.GetPeers().OrderByDescending(p => p.CurrentBlockHeight).FirstOrDefault();
            return Task.FromResult(peer?.CurrentBlockHeight ?? 0);
        }

    }
}