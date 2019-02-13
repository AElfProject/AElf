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
        private ILogger<GrpcNetworkServer> _logger;
        
        private IAElfNetworkServer _server;

        public GrpcNetworkService(IAElfNetworkServer server)
        {
            _server = server;
            _logger = NullLogger<GrpcNetworkServer>.Instance;
        }

        public void AddPeer(string address)
        {
            _server.AddPeerAsync(address);
        }

        public Task RemovePeer(string address)
        {
            return Task.FromResult(_server.RemovePeerAsync(address));
        }

        public List<string> GetPeers()
        {
            return _server.GetPeers().Select(p => p.PeerAddress).ToList();
        }

        public async Task BroadcastAnnounce(Block b)
        {
            foreach (var peer in _server.GetPeers())
            {
                try
                {
                    await peer.AnnounceAsync(new Announcement { Id = ByteString.CopyFrom(b.GetHashBytes()) });
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error while sending block."); // todo improve
                }
            }
        }

        public async Task BroadcastTransaction(Transaction tx)
        {
            foreach (var peer in _server.GetPeers())
            {
                try
                {
                    await peer.SendTransactionAsync(tx);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error while sending transaction."); // todo improve
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
            foreach (var p in _server.GetPeers())
            {
                try
                {
                    if (p == null)
                    {
                        _logger.LogWarning("No peers left.");
                        return null;
                    }
            
                    _logger.LogDebug($"Attempting get with {p}");

                    BlockReply block = await p.RequestBlockAsync(request);

                    if (block.Block != null)
                        return block.Block;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error while requesting block.");
                }
            }

            return null;
        }
    }
}