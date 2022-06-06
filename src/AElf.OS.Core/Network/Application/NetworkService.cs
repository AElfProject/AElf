using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Consensus.Application;
using AElf.OS.Network.Helpers;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Network.Types;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Application;

/// <summary>
///     Exposes networking functionality to the application handlers.
/// </summary>
public class NetworkService : INetworkService, ISingletonDependency
{
    private readonly IBlackListedPeerProvider _blackListedPeerProvider;
    private readonly IBroadcastPrivilegedPubkeyListProvider _broadcastPrivilegedPubkeyListProvider;
    private readonly IAElfNetworkServer _networkServer;
    private readonly IPeerPool _peerPool;
    private readonly ITaskQueueManager _taskQueueManager;

    public NetworkService(IPeerPool peerPool, ITaskQueueManager taskQueueManager, IAElfNetworkServer networkServer,
        IBlackListedPeerProvider blackListedPeerProvider,
        IBroadcastPrivilegedPubkeyListProvider broadcastPrivilegedPubkeyListProvider)
    {
        _peerPool = peerPool;
        _taskQueueManager = taskQueueManager;
        _networkServer = networkServer;
        _broadcastPrivilegedPubkeyListProvider = broadcastPrivilegedPubkeyListProvider;
        _blackListedPeerProvider = blackListedPeerProvider;

        Logger = NullLogger<NetworkService>.Instance;
    }

    public ILogger<NetworkService> Logger { get; set; }

    public async Task<bool> AddPeerAsync(string endpoint)
    {
        return await TryAddPeerAsync(endpoint, false);
    }

    /// <summary>
    ///     Add trusted peer, will remove the host from blacklist first.
    /// </summary>
    /// <param name="endpoint"></param>
    /// <returns></returns>
    public async Task<bool> AddTrustedPeerAsync(string endpoint)
    {
        return await TryAddPeerAsync(endpoint, true);
    }

    public async Task<bool> RemovePeerByEndpointAsync(string endpoint,
        int removalSeconds = NetworkConstants.DefaultPeerRemovalSeconds)
    {
        if (!AElfPeerEndpointHelper.TryParse(endpoint, out var aelfPeerEndpoint))
            return false;

        var peer = _peerPool.FindPeerByEndpoint(aelfPeerEndpoint);
        if (!await TryRemovePeerAsync(peer, removalSeconds))
        {
            Logger.LogWarning($"Remove peer failed. Peer address: {endpoint}");
            return false;
        }

        return true;
    }

    public async Task<bool> RemovePeerByPubkeyAsync(string peerPubkey,
        int removalSeconds = NetworkConstants.DefaultPeerRemovalSeconds)
    {
        var peer = _peerPool.FindPeerByPublicKey(peerPubkey);
        if (!await TryRemovePeerAsync(peer, removalSeconds))
        {
            Logger.LogWarning($"Remove peer failed. Peer pubkey: {peerPubkey}");
            return false;
        }

        return true;
    }

    public List<PeerInfo> GetPeers(bool includeFailing = true)
    {
        return _peerPool.GetPeers(includeFailing).Select(PeerInfoHelper.FromNetworkPeer).ToList();
    }

    public PeerInfo GetPeerByPubkey(string peerPubkey)
    {
        var peer = _peerPool.FindPeerByPublicKey(peerPubkey);
        return peer == null ? null : PeerInfoHelper.FromNetworkPeer(peer);
    }

    public async Task BroadcastBlockWithTransactionsAsync(BlockWithTransactions blockWithTransactions)
    {
        if (IsOldBlock(blockWithTransactions.Header))
            return;

        var nextMinerPubkey = await GetNextMinerPubkey(blockWithTransactions.Header);

        var nextPeer = _peerPool.FindPeerByPublicKey(nextMinerPubkey);
        if (nextPeer != null)
            EnqueueBlock(nextPeer, blockWithTransactions);

        foreach (var peer in _peerPool.GetPeers())
        {
            if (nextPeer != null && peer.Info.Pubkey == nextPeer.Info.Pubkey)
                continue;

            EnqueueBlock(peer, blockWithTransactions);
        }
    }

    public Task BroadcastAnnounceAsync(BlockHeader blockHeader)
    {
        var blockHash = blockHeader.GetHash();

        if (IsOldBlock(blockHeader))
            return Task.CompletedTask;

        var blockAnnouncement = new BlockAnnouncement
        {
            BlockHash = blockHash,
            BlockHeight = blockHeader.Height
        };

        foreach (var peer in _peerPool.GetPeers())
            try
            {
                if (peer.KnowsBlock(blockHash))
                    continue; // block already known to this peer

                peer.EnqueueAnnouncement(blockAnnouncement, async ex =>
                {
                    peer.TryAddKnownBlock(blockHash);
                    if (ex != null)
                    {
                        Logger.LogInformation(ex, $"Could not broadcast announcement to {peer} " +
                                                  $"- status {peer.ConnectionStatus}.");

                        await HandleNetworkException(peer, ex);
                    }
                });
            }
            catch (NetworkException ex)
            {
                Logger.LogWarning(ex, $"Could not enqueue announcement to {peer} " +
                                      $"- status {peer.ConnectionStatus}.");
            }

        return Task.CompletedTask;
    }

    public Task BroadcastTransactionAsync(Transaction transaction)
    {
        var txHash = transaction.GetHash();
        foreach (var peer in _peerPool.GetPeers())
            try
            {
                if (peer.KnowsTransaction(txHash))
                    continue; // transaction already known to this peer

                peer.EnqueueTransaction(transaction, async ex =>
                {
                    if (ex != null)
                    {
                        Logger.LogWarning(ex, $"Could not broadcast transaction to {peer} " +
                                              $"- status {peer.ConnectionStatus}.");

                        await HandleNetworkException(peer, ex);
                    }
                });
            }
            catch (NetworkException ex)
            {
                Logger.LogWarning(ex, $"Could not enqueue transaction to {peer} - " +
                                      $"status {peer.ConnectionStatus}.");
            }

        return Task.CompletedTask;
    }

    public Task BroadcastLibAnnounceAsync(Hash libHash, long libHeight)
    {
        var announce = new LibAnnouncement
        {
            LibHash = libHash,
            LibHeight = libHeight
        };

        foreach (var peer in _peerPool.GetPeers())
            try
            {
                peer.EnqueueLibAnnouncement(announce, async ex =>
                {
                    if (ex != null)
                    {
                        Logger.LogWarning(ex, $"Could not broadcast lib announcement to {peer} " +
                                              $"- status {peer.ConnectionStatus}.");
                        await HandleNetworkException(peer, ex);
                    }
                });
            }
            catch (NetworkException ex)
            {
                Logger.LogWarning(ex, $"Could not enqueue lib announcement to {peer} " +
                                      $"- status {peer.ConnectionStatus}.");
            }

        return Task.CompletedTask;
    }

    public async Task CheckPeersHealthAsync()
    {
        foreach (var peer in _peerPool.GetPeers(true))
        {
            Logger.LogDebug($"Health checking: {peer}");

            if (peer.IsInvalid)
            {
                _peerPool.RemovePeer(peer.Info.Pubkey);
                await peer.DisconnectAsync(false);
                Logger.LogInformation($"Remove invalid peer: {peer}");
                continue;
            }

            try
            {
                await peer.CheckHealthAsync();
            }
            catch (NetworkException ex)
            {
                if (ex.ExceptionType == NetworkExceptionType.Unrecoverable
                    || ex.ExceptionType == NetworkExceptionType.PeerUnstable)
                {
                    Logger.LogInformation(ex, $"Removing unhealthy peer {peer}.");
                    await _networkServer.TrySchedulePeerReconnectionAsync(peer);
                }
            }
        }
    }

    public void CheckNtpDrift()
    {
        _networkServer.CheckNtpDrift();
    }

    public async Task<Response<List<BlockWithTransactions>>> GetBlocksAsync(Hash previousBlock, int count,
        string peerPubkey)
    {
        var peer = _peerPool.FindPeerByPublicKey(peerPubkey);

        if (peer == null)
            throw new InvalidOperationException($"Could not find peer {peerPubkey}.");

        var response = await Request(peer, p => p.GetBlocksAsync(previousBlock, count));

        if (response.Success && response.Payload != null
                             && (response.Payload.Count == 0 || response.Payload.Count != count))
            Logger.LogDebug($"Requested blocks from {peer} - count miss match, " +
                            $"asked for {count} but got {response.Payload.Count} (from {previousBlock})");

        return response;
    }

    public async Task<Response<BlockWithTransactions>> GetBlockByHashAsync(Hash hash, string peerPubkey)
    {
        var peer = _peerPool.FindPeerByPublicKey(peerPubkey);

        if (peer == null)
            throw new InvalidOperationException($"Could not find peer {peerPubkey}.");

        Logger.LogDebug($"Getting block by hash, hash: {hash} from {peer}.");

        return await Request(peer, p => p.GetBlockByHashAsync(hash));
    }

    public bool IsPeerPoolFull()
    {
        return _peerPool.IsFull();
    }

    private async Task<bool> TryAddPeerAsync(string endpoint, bool isTrusted)
    {
        if (!AElfPeerEndpointHelper.TryParse(endpoint, out var aelfPeerEndpoint))
        {
            Logger.LogDebug($"Could not parse endpoint {endpoint}.");
            return false;
        }

        if (isTrusted)
            _blackListedPeerProvider.RemoveHostFromBlackList(aelfPeerEndpoint.Host);

        return await _networkServer.ConnectAsync(aelfPeerEndpoint);
    }

    /// <summary>
    ///     Try remove the peer, put the peer to blacklist, and disconnect.
    /// </summary>
    /// <param name="peer"></param>
    /// <param name="removalSeconds"></param>
    /// <returns>If the peer is null, return false.</returns>
    private async Task<bool> TryRemovePeerAsync(IPeer peer, int removalSeconds)
    {
        if (peer == null) return false;

        _blackListedPeerProvider.AddHostToBlackList(peer.RemoteEndpoint.Host, removalSeconds);
        Logger.LogDebug($"Blacklisted {peer.RemoteEndpoint.Host} ({peer.Info.Pubkey})");

        await _networkServer.DisconnectAsync(peer);

        return true;
    }

    private bool IsOldBlock(BlockHeader header)
    {
        var limit = TimestampHelper.GetUtcNow()
                    - TimestampHelper.DurationFromMinutes(NetworkConstants.DefaultMaxBlockAgeToBroadcastInMinutes);

        if (header.Time < limit)
            return true;

        return false;
    }

    private void EnqueueBlock(IPeer peer, BlockWithTransactions blockWithTransactions)
    {
        try
        {
            var blockHash = blockWithTransactions.GetHash();

            if (peer.KnowsBlock(blockHash))
                return; // block already known to this peer

            peer.EnqueueBlock(blockWithTransactions, async ex =>
            {
                peer.TryAddKnownBlock(blockHash);

                if (ex != null)
                {
                    Logger.LogWarning(ex, $"Could not broadcast block to {peer} - status {peer.ConnectionStatus}.");
                    await HandleNetworkException(peer, ex);
                }
            });
        }
        catch (NetworkException ex)
        {
            Logger.LogWarning(ex, $"Could not enqueue block to {peer} - status {peer.ConnectionStatus}.");
        }
    }

    private async Task<string> GetNextMinerPubkey(BlockHeader blockHeader)
    {
        var broadcastList = await _broadcastPrivilegedPubkeyListProvider.GetPubkeyList(blockHeader);
        return broadcastList.IsNullOrEmpty() ? null : broadcastList[0];
    }

    private async Task<Response<T>> Request<T>(IPeer peer, Func<IPeer, Task<T>> func) where T : class
    {
        try
        {
            return new Response<T>(await func(peer));
        }
        catch (NetworkException ex)
        {
            Logger.LogWarning(ex, $"Request failed from {peer.RemoteEndpoint}.");

            if (ex.ExceptionType == NetworkExceptionType.HandlerException)
                return new Response<T>(default);

            await HandleNetworkException(peer, ex);
        }

        return new Response<T>();
    }

    private async Task HandleNetworkException(IPeer peer, NetworkException exception)
    {
        if (exception.ExceptionType == NetworkExceptionType.Unrecoverable)
        {
            Logger.LogInformation(exception, $"Removing unrecoverable {peer}.");
            await _networkServer.TrySchedulePeerReconnectionAsync(peer);
        }
        else if (exception.ExceptionType == NetworkExceptionType.PeerUnstable)
        {
            Logger.LogDebug(exception, $"Queuing peer for reconnection {peer.RemoteEndpoint}.");
            QueueNetworkTask(async () => await RecoverPeerAsync(peer));
        }
    }

    private async Task RecoverPeerAsync(IPeer peer)
    {
        if (peer.IsReady) // peer recovered already
            return;

        var success = await peer.TryRecoverAsync();

        if (!success)
            await _networkServer.TrySchedulePeerReconnectionAsync(peer);
    }

    private void QueueNetworkTask(Func<Task> task)
    {
        _taskQueueManager.Enqueue(task, NetworkConstants.PeerReconnectionQueueName);
    }
}