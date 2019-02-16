using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
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
        
        public async Task<IBlock> GetBlockByHeight(ulong height, string peer = null)
        {
            return await GetBlock(new BlockRequest { BlockNumber = (long)height });
        }

        public async Task<IBlock> GetBlockByHash(Hash hash, string peer = null)
        {
            return await GetBlock(new BlockRequest { Id = hash.Value });
        }

        private async Task<IBlock> GetBlock(BlockRequest request, string peer = null)
        {
            // todo use peer if specified
            foreach (var p in _peerPool.GetPeers())
            {
                try
                {
                    if (p == null)
                    {
                        Logger.LogWarning("No peers left.");
                        return null;
                    }
            
                    Logger.LogDebug($"Attempting get with {p}");

                    BlockReply block = await p.RequestBlockAsync(request);

                    if (block.Block != null)
                        return block.Block;
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Error while requesting block.");
                }
            }

            return null;
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