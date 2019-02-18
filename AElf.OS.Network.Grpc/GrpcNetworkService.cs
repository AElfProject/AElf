using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Storages;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Grpc
{
    public class GrpcNetworkService : INetworkService, ISingletonDependency
    {
        private readonly IPeerPool _peerPool;
        
        public ILogger<GrpcServerService> Logger { get; set; }

        public GrpcNetworkService(IPeerPool peerPool)
        {
            _peerPool = peerPool;
            
            Logger = NullLogger<GrpcServerService>.Instance;
        }

        public async Task AddPeerAsync(string address)
        {
            await _peerPool.AddPeerAsync(address);
        }

        public async Task<bool> RemovePeerAsync(string address)
        {
            return await _peerPool.RemovePeerAsync(address);
        }

        public List<string> GetPeers()
        {
            return _peerPool.GetPeers().Select(p => p.PeerAddress).ToList();
        }

        public async Task BroadcastAnnounce(BlockHeader blockHeader)
        {
            foreach (var peer in _peerPool.GetPeers())
            {
                try
                {
                    await peer.AnnounceAsync(new Announcement { Header = blockHeader });
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Error while sending block."); // todo improve
                }
            }
        }

        public async Task BroadcastTransaction(Transaction tx)
        {
            foreach (var peer in _peerPool.GetPeers())
            {
                try
                {
                    await peer.SendTransactionAsync(tx);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Error while sending transaction."); // todo improve
                }
            }
        }
        
        public async Task<IBlock> GetBlockByHeight(ulong height, string peer = null, bool tryOthersIfSpecifiedFails = false)
        {
            Logger.LogDebug($"Getting block by height, height: {height} from {peer}.");
            return await GetBlock(new BlockRequest { Height = (long)height }, peer, tryOthersIfSpecifiedFails);
        }

        public async Task<IBlock> GetBlockByHash(Hash hash, string peer = null, bool tryOthersIfSpecifiedFails = false)
        {
            Logger.LogDebug($"Getting block by hash, hash: {hash} from {peer}.");
            return await GetBlock(new BlockRequest { Id = hash.Value }, peer, tryOthersIfSpecifiedFails);
        }

        /// <summary>
        /// Requests a block from a peer/peers. The parameter permit the following scenarios:
        /// (request, null, _ ) :  request from every peer until found (try others ignored).
        /// (request, peer, false) : request from 'peer' only.
        /// (request, peer, true) : request first from 'peer' and if fails try others.
        /// </summary>
        private async Task<IBlock> GetBlock(BlockRequest request, string peer = null, bool tryOthersIfSpecifiedFails = false)
        {
            if (tryOthersIfSpecifiedFails && string.IsNullOrWhiteSpace(peer))
                throw new InvalidOperationException($"Parameter {nameof(tryOthersIfSpecifiedFails)} cannot be true, " +
                                                    $"if no fallback peer is specified.");
            
            // try get the block from the specified peer. 
            if (!string.IsNullOrWhiteSpace(peer))
            {
                GrpcPeer p = _peerPool.FindPeer(peer);
                
                if (p == null)
                {
                    // if the peer was specified but we can't find it 
                    // we don't try any further.
                    Logger.LogWarning($"Specified peer was not found.");
                    return null; 
                }
                
                var blck = await RequestBlockTo(request, p);

                if (blck != null)
                    return blck;
                
                if (!tryOthersIfSpecifiedFails)
                {
                    Logger.LogWarning($"{peer} does not have block {nameof(tryOthersIfSpecifiedFails)} is false.");
                    return null;
                }
            }
            
            foreach (var p in _peerPool.GetPeers())
            {
                Block block = await RequestBlockTo(request, p);

                if (block != null)
                    return block;
            }

            return null;
        }

        private async Task<Block> RequestBlockTo(BlockRequest request, GrpcPeer peer)
        {
            try
            {
                BlockReply block = await peer.RequestBlockAsync(request);
                return block?.Block;
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error while requesting block from {peer.PeerAddress}.");
                return null;
            }
        }

        public async Task<List<Hash>> GetBlockIds(Hash topHash, int count, string peer)
        {
            GrpcPeer grpcPeer = _peerPool.FindPeer(peer);
            var blockIds = await grpcPeer.GetBlockIds(new BlockIdsRequest { 
                FirstBlockId = topHash.Value,  
                Count = count
            });

            return blockIds.Ids.Select(id => Hash.FromRawBytes(id.ToByteArray())).ToList();
        }
    }
}