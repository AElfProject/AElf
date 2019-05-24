using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
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

        public List<IPeer> GetPeers()
        {
            return _peerPool.GetPeers(true).ToList();
        }

        public async Task<int> BroadcastAnnounceAsync(BlockHeader blockHeader)
        {
            int successfulBcasts = 0;
            
            var announce = new PeerNewBlockAnnouncement
            {
                BlockHash = blockHeader.GetHash(),
                BlockHeight = blockHeader.Height,
                BlockTime = blockHeader.Time
            };
            
            var peers = _peerPool.GetPeers().ToList();

            _peerPool.AddRecentBlockHeightAndHash(blockHeader.Height, blockHeader.GetHash());
            await Task.WhenAll(peers.Select(async peer =>
            {
                try
                {
                    Logger.LogDebug($"Block announced: {announce}");
                    await peer.AnnounceAsync(announce);
                    
                    Interlocked.Increment(ref successfulBcasts);
                }
                catch (NetworkException e)
                {
                    Logger.LogError(e, "Error while sending block.");
                }
            }));
            
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

        public async Task<List<BlockWithTransactions>> GetBlocksAsync(Hash previousBlock, long previousHeight, int count, string peerPubKey = null, bool tryOthersIfFail = false)
        {
            if (string.IsNullOrWhiteSpace(peerPubKey))
                throw new InvalidOperationException();

            var peers = SelectPeers(peerPubKey);
               
            var blocks = await RequestAsync(peers, p => p.GetBlocksAsync(previousBlock, count));

            return blocks;
            
            // try get the block from the specified peer. 
//            if (!string.IsNullOrWhiteSpace(peerPubKey))
//            {
//                IPeer peer = _peerPool.FindPeerByPublicKey(peerPubKey);
//
//                if (peer == null)
//                {
//                    // if the peer was specified but we can't find it 
//                    // we don't try any further.
//                    Logger.LogWarning($"Specified peer was not found.");
//                    return null;
//                }
//
//                var blocks = await RequestAsync(peer, p => p.GetBlocksAsync(previousBlock, count));
//
//                if (blocks != null && blocks.Count > 0)
//                    return blocks;
//
//                if (!tryOthersIfFail)
//                {
//                    Logger.LogWarning($"{peerPubKey} does not have blocks {nameof(tryOthersIfFail)} is false.");
//                    return null;
//                }
//            }
            
            // shuffle the peers that can give us the blocks
//            var shuffledPeers = _peerPool.GetPeers()
//                .Where(p => p.CurrentBlockHeight >= previousHeight)
//                .OrderBy(a => Guid.NewGuid());
//                
//            foreach (var peer in shuffledPeers)
//            {
//                var blocks = await RequestAsync(peer, p => p.GetBlocksAsync(previousBlock, count));
//
//                if (blocks != null)
//                    return blocks;
//            }

            return null;
        }

        private List<IPeer> SelectPeers(string peerPubKey)
        {
            List<IPeer> peers = new List<IPeer>();
            
            // Get the suggested peer 
            IPeer suggestedPeer = _peerPool.FindPeerByPublicKey(peerPubKey);

            if (suggestedPeer == null)
                Logger.LogWarning("Could not find suggested peer");
            else
                peers.Add(suggestedPeer);
            
            // Get our best peer
            IPeer bestPeer = _peerPool.GetBestPeer();
            
            if (bestPeer == null)
                Logger.LogWarning("No best peer.");
            else
                peers.Add(bestPeer);
            
            Random rnd = new Random();
            
            // Fill with random peers.
            List<IPeer> randomPeers = _peerPool.GetPeers()
                .Where(p => p.PubKey != peerPubKey && (bestPeer == null || p.PubKey != bestPeer.PubKey))
                .OrderBy(x => rnd.Next())
                .Take(3 - peers.Count)
                .ToList();
            
            peers.AddRange(randomPeers);
            
            Logger.LogDebug($"Selected {peers.Count} for the request.");

            return peers;
        }

        public async Task<BlockWithTransactions> GetBlockByHashAsync(Hash hash, string peer = null, bool tryOthersIfSpecifiedFails = false)
        {
            Logger.LogDebug($"Getting block by hash, hash: {hash} from {peer}.");
            return await GetBlockAsync(hash, peer, tryOthersIfSpecifiedFails);
        }

        private async Task<BlockWithTransactions> GetBlockAsync(Hash hash, string peer = null, bool tryOthersIfSpecifiedFails = false)
        {
            if (tryOthersIfSpecifiedFails && string.IsNullOrWhiteSpace(peer))
                throw new InvalidOperationException($"Parameter {nameof(tryOthersIfSpecifiedFails)} cannot be true, " +
                                                    $"if no fallback peer is specified.");
            
            var peers = SelectPeers(peer);
            var block = await RequestBlockToAsync(hash, peers);
            return block;
            
            // try get the block from the specified peer. 
//            if (!string.IsNullOrWhiteSpace(peer))
//            {
//                IPeer p = _peerPool.FindPeerByPublicKey(peer);
//
//                if (p == null)
//                {
//                    // if the peer was specified but we can't find it 
//                    // we don't try any further.
//                    Logger.LogWarning($"Specified peer was not found.");
//                    return null;
//                }
//
//                var block = await RequestBlockToAsync(hash, p);
//
//                if (block != null)
//                    return block;
//
//                if (!tryOthersIfSpecifiedFails)
//                {
//                    Logger.LogWarning($"{peer} does not have block {nameof(tryOthersIfSpecifiedFails)} is false.");
//                    return null;
//                }
//            }
//
//            foreach (var p in _peerPool.GetPeers())
//            {
//                BlockWithTransactions block = await RequestBlockToAsync(hash, p);
//
//                if (block != null)
//                    return block;
//            }

            return null;
        }

        private async Task<BlockWithTransactions> RequestBlockToAsync(Hash hash, List<IPeer> peers)
        {
            return await RequestAsync(peers, p => p.RequestBlockAsync(hash));
        }

        private async Task<T> RequestAsync<T>(List<IPeer> peers, Func<IPeer, Task<T>> func) where T : class
        {
            var taskList = peers.Select(async peer =>
            {
                try
                {
                    var res = await func(peer);
                    return (peer, res);
                }
                catch (NetworkException e)
                {
                    Logger.LogError(e, $"Error while requesting block from {peer.PeerIpAddress}.");
                    return (peer, null);
                }
                
            }).ToList();

            Task<(IPeer, T)> finished = null; 
                
            while (taskList.Count > 0)
            {
                var next = await Task.WhenAny(taskList.ToArray<Task<(IPeer, T)>>());

                if (next.Result.Item2 != null)
                {
                    finished = next;
                    break;
                }
                
                taskList.Remove(next);
            }

            if (finished == null)
                return null;
            
            if (finished.Result.Item1 != null)
            {
                var peer = finished.Result.Item1;
                
                Logger.LogDebug($"First replied : {peer}.");
                
                if (!peer.IsBest)
                {
                    Logger.LogDebug($"New best peer found: {peer}.");

                    foreach (var peerToReset in _peerPool.GetPeers(true))
                    {
                        peerToReset.IsBest = false;
                    }
                    
                    peer.IsBest = true;
                }
            }
            
            return finished.Result.Item2;
            
//            var taskList = peers.Select(async peer =>
//            {
//                try
//                {
//                    return await func(peer);
//                }
//                catch (NetworkException e)
//                {
//                    Logger.LogError(e, $"Error while requesting block from {peer.PeerIpAddress}.");
//                    return null;
//                }
//                
//            }).ToList();
//            
//            var finished = await Task.WhenAny(taskList.ToArray<Task<T>>());
//
//            return finished.Result;


//                try
//                {
//                    return await func(peer);
//                }
//                catch (NetworkException e)
//                {
//                    Logger.LogError(e, $"Error while requesting block from {peer.PeerIpAddress}.");
//                    return null;
//                }

        }

        public Task<long> GetBestChainHeightAsync(string peerPubKey = null)
        {
            var peer = !peerPubKey.IsNullOrEmpty()
                ? _peerPool.FindPeerByPublicKey(peerPubKey)
                : _peerPool.GetPeers().OrderByDescending(p => p.CurrentBlockHeight).FirstOrDefault();
            
            return Task.FromResult(peer?.CurrentBlockHeight ?? 0);
        }
    }

    public static class TaskExtensions
    {
        public static Task<T> GetFirstResult<T>(
            ICollection<Func<CancellationToken, Task<T>>> taskFactories, 
            Predicate<T> predicate) where T : class
        {
            var tcs = new TaskCompletionSource<T>();
            var cts = new CancellationTokenSource();

            int completedCount = 0;
            // in case you have a lot of tasks you might need to throttle them 
            //(e.g. so you don't try to send 99999999 requests at the same time)
            // see: http://stackoverflow.com/a/25877042/67824
            foreach (var taskFactory in taskFactories)
            {
                taskFactory(cts.Token).ContinueWith(t => 
                {
                    if (t.Exception != null)
                    {
                        Console.WriteLine($"Task completed with exception: {t.Exception}");
                    }
                    else if (predicate(t.Result))
                    {
                        cts.Cancel();
                        tcs.TrySetResult(t.Result);
                    }

                    if (Interlocked.Increment(ref completedCount) == taskFactories.Count)
                    {
                        tcs.SetException(new InvalidOperationException("All tasks failed"));
                    }

                }, cts.Token);
            }

            return tcs.Task;
        }
    }
}