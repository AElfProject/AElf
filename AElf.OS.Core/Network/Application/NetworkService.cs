using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Application
{
    
    //TODO: should wrap grpc exception in NetworkException as innerException
    [Serializable]
    public class NetworkException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public NetworkException()
        {
        }

        public NetworkException(string message) : base(message)
        {
        }

        public NetworkException(string message, Exception inner) : base(message, inner)
        {
        }

        protected NetworkException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }

    public class NetworkService : INetworkService, ISingletonDependency
    {
        private readonly IPeerPool _peerPool;

        public ILogger<NetworkService> Logger { get; set; }

        public NetworkService(IPeerPool peerPool)
        {
            _peerPool = peerPool;

            Logger = NullLogger<NetworkService>.Instance;
        }

        //TODO: Add Peer operation test cases [Case]
        public async Task<bool> AddPeerAsync(string address)
        {
            return await _peerPool.AddPeerAsync(address);
        }

        public async Task<bool> RemovePeerAsync(string address)
        {
            return await _peerPool.RemovePeerAsync(address);
        }

        public List<string> GetPeers()
        {
            return _peerPool.GetPeers().Select(p => p.PeerIpAddress).ToList();
        }

        public async Task BroadcastAnnounceAsync(BlockHeader blockHeader)
        {
            foreach (var peer in _peerPool.GetPeers())
            {
                try
                {
                    await peer.AnnounceAsync(new PeerNewBlockAnnouncement
                        {BlockHash = blockHeader.GetHash(), BlockHeight = blockHeader.Height});
                }
                catch (NetworkException e)
                {
                    Logger.LogError(e, "Error while sending block."); // todo improve
                }
            }
        }

        public async Task BroadcastTransactionAsync(Transaction tx)
        {
            foreach (var peer in _peerPool.GetPeers())
            {
                try
                {
                    await peer.SendTransactionAsync(tx);
                }
                catch (NetworkException e)
                {
                    Logger.LogError(e, "Error while sending transaction."); // todo improve
                }
            }
        }

        //TODO: Add GetBlockAsync from specified peerPubKey case [Case]
        public async Task<List<Block>> GetBlocksAsync(Hash blockHash, int count, string peerPubKey = null,
            bool tryOthersIfFail = false)
        {
            // try get the block from the specified peer. 
            if (!string.IsNullOrWhiteSpace(peerPubKey))
            {
                IPeer p = _peerPool.FindPeerByPublicKey(peerPubKey);

                if (p == null)
                {
                    // if the peer was specified but we can't find it 
                    // we don't try any further.
                    Logger.LogWarning($"Specified peer was not found.");
                    return null;
                }

                var blocks = await p.GetBlocksAsync(blockHash, count);

                if (blocks != null)
                    return blocks;

                if (!tryOthersIfFail)
                {
                    Logger.LogWarning($"{peerPubKey} does not have block {nameof(tryOthersIfFail)} is false.");
                    return null;
                }
            }

            foreach (var p in _peerPool.GetPeers())
            {
                var blocks = await p.GetBlocksAsync(blockHash, count);

                if (blocks != null)
                    return blocks;
            }

            return null;
        }

        public async Task<Block> GetBlockByHashAsync(Hash hash, string peer = null,
            bool tryOthersIfSpecifiedFails = false)
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
                IPeer p = _peerPool.FindPeerByAddress(peer);

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
            
            
            //TODO: change like this @sam
            return await RequestAsync(peer, p => p.RequestBlockAsync(hash));
            //Remove
            /*
            return await RequestAsync(peer, p =>
            {
                
                //More logic here
                return p.RequestBlockAsync(hash);
            });
            
            try
            {
                return await peer.RequestBlockAsync(hash);
            }
            catch (NetworkException e)
            {
                Logger.LogError(e, $"Error while requesting block from {peer.PeerIpAddress}.");
                return null;
            }*/
        }

        private async Task<T> RequestAsync<T>(IPeer peer, Func<IPeer, Task<T>> func)
            where T : class
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
    }
}