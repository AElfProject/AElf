using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.Network.Application;
using AElf.OS.Network.Events;
using AElf.OS.Network.Grpc.Helpers;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Network.Protocol;
using AElf.OS.Network.Types;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.EventBus.Local;

namespace AElf.OS.Network.Grpc;

public class ConnectionService : IConnectionService
{
    private readonly IHandshakeProvider _handshakeProvider;
    private readonly IPeerDialer _peerDialer;

    private readonly IPeerPool _peerPool;
    private readonly IReconnectionService _reconnectionService;

    public ConnectionService(IPeerPool peerPool, IPeerDialer peerDialer,
        IHandshakeProvider handshakeProvider, IReconnectionService reconnectionService)
    {
        _peerPool = peerPool;
        _peerDialer = peerDialer;
        _handshakeProvider = handshakeProvider;
        _reconnectionService = reconnectionService;

        Logger = NullLogger<ConnectionService>.Instance;
        EventBus = NullLocalEventBus.Instance;
    }

    private NetworkOptions NetworkOptions => NetworkOptionsSnapshot.Value;
    public IOptionsSnapshot<NetworkOptions> NetworkOptionsSnapshot { get; set; }
    public ILocalEventBus EventBus { get; set; }
    public ILogger<ConnectionService> Logger { get; set; }

    public async Task DisconnectAsync(IPeer peer, bool sendDisconnect = false)
    {
        // clean the pool
        if (_peerPool.RemovePeer(peer.Info.Pubkey) == null)
            Logger.LogWarning($"{peer} was not found in pool.");

        // cancel any pending reconnection
        _reconnectionService.CancelReconnection(peer.RemoteEndpoint.ToString());

        // dispose the peer
        await peer.DisconnectAsync(sendDisconnect);

        Logger.LogInformation($"Removed peer {peer}");
    }

    public Task<bool> SchedulePeerReconnection(DnsEndPoint endpoint)
    {
        return Task.FromResult(_reconnectionService.SchedulePeerForReconnection(endpoint.ToString()));
    }

    public async Task<bool> TrySchedulePeerReconnectionAsync(IPeer peer)
    {
        await DisconnectAsync(peer);

        if (peer.Info.IsInbound && (NetworkOptions.BootNodes == null || !NetworkOptions.BootNodes.Any()
                                                                     || !NetworkOptions.BootNodes.Contains(
                                                                         peer.RemoteEndpoint.ToString())))
        {
            Logger.LogDebug($"Completely dropping {peer.RemoteEndpoint} (inbound: {peer.Info.IsInbound}).");
            return false;
        }

        return _reconnectionService.SchedulePeerForReconnection(peer.RemoteEndpoint.ToString());
    }

    public GrpcPeer GetPeerByPubkey(string pubkey)
    {
        return _peerPool.FindPeerByPublicKey(pubkey) as GrpcPeer;
    }

    /// <summary>
    ///     Connects to a node with the given ip address and adds it to the node's peer pool.
    /// </summary>
    /// <param name="endpoint">the ip address of the distant node</param>
    /// <returns>True if the connection was successful, false otherwise</returns>
    public async Task<bool> ConnectAsync(DnsEndPoint endpoint)
    {
        Logger.LogDebug($"Attempting to reach {endpoint}.");

        var dialedPeer = await GetDialedPeerWithEndpointAsync(endpoint);
        if (dialedPeer == null) return false;

        var inboundPeer = _peerPool.FindPeerByPublicKey(dialedPeer.Info.Pubkey) as GrpcPeer;

        /* A connection already exists, this can happen when both peers dial each other at the same time. To make
         sure both sides close the same connection, they both decide based on the times of the handshakes.
         Scenario steps, chronologically:
            1) P1 (hsk_time: t1) --> dials P2 --and-- P1 <-- P2 dials (hsk_time: t2)
            2) P2 receives P1s dial with t1 (in the hsk) and add to the pool
            3) P1 receives P2s dial with and adds to pool
            4) both dials finish and find that the pool already contains the dialed node.
        To resolve this situation, both peers will choose the connection that was initiated the earliest, 
        so either P1s dial or P2s. */

        var currentPeer = dialedPeer;
        if (inboundPeer != null)
        {
            Logger.LogDebug("Duplicate peer connection detected: " +
                            $"{inboundPeer} ({inboundPeer.LastReceivedHandshakeTime}) " +
                            $"vs {dialedPeer} ({dialedPeer.LastSentHandshakeTime}).");

            if (inboundPeer.LastReceivedHandshakeTime > dialedPeer.LastSentHandshakeTime)
            {
                // we started the dial first, replace the inbound connection with the dialed 
                if (!_peerPool.TryReplace(inboundPeer.Info.Pubkey, inboundPeer, dialedPeer))
                    Logger.LogWarning("Replacing the inbound connection failed.");

                await inboundPeer.DisconnectAsync(false);

                Logger.LogDebug($"Replaced the inbound connection with the dialed peer {inboundPeer} .");
            }
            else
            {
                // keep the inbound connection
                await dialedPeer.DisconnectAsync(false);
                currentPeer = inboundPeer;

                Logger.LogDebug($"Disconnected dialed peer {dialedPeer}.");
            }
        }
        else
        {
            if (!_peerPool.TryAddPeer(dialedPeer))
            {
                Logger.LogWarning($"Peer add to the failed {dialedPeer.Info.Pubkey}.");
                await dialedPeer.DisconnectAsync(false);
                return false;
            }

            Logger.LogDebug($"Added to pool {dialedPeer.RemoteEndpoint} - {dialedPeer.Info.Pubkey}.");
        }

        try
        {
            await currentPeer.ConfirmHandshakeAsync();
        }
        catch (Exception e)
        {
            Logger.LogDebug(e, $"Confirm handshake error. Peer: {currentPeer.Info.Pubkey}.");
            _peerPool.RemovePeer(currentPeer.Info.Pubkey);
            await currentPeer.DisconnectAsync(false);
            throw;
        }

        currentPeer.IsConnected = true;
        currentPeer.SyncState = SyncState.Syncing;

        Logger.LogInformation(
            $"Connected to: {currentPeer.RemoteEndpoint} - {currentPeer.Info.Pubkey.Substring(0, 45)}" +
            $" - in-token {currentPeer.InboundSessionId?.ToHex()}, out-token {currentPeer.OutboundSessionId?.ToHex()}" +
            $" - LIB height {currentPeer.LastKnownLibHeight}" +
            $" - best chain [{currentPeer.CurrentBlockHeight}, {currentPeer.CurrentBlockHash}]");

        FireConnectionEvent(currentPeer);

        return true;
    }

    private async Task<HandshakeReply> ValidateHandshakeAsync(DnsEndPoint endpoint, Handshake handshake)
    {
        // validate the handshake (signature, chain id...)
        var handshakeValidationResult = await _handshakeProvider.ValidateHandshakeAsync(handshake);
        if (handshakeValidationResult != HandshakeValidationResult.Ok)
        {
            var handshakeError = GetHandshakeError(handshakeValidationResult);
            return new HandshakeReply { Error = handshakeError };
        }

        Logger.LogDebug($"peer {endpoint} sent a valid handshake {handshake}");
        return null;
    }

    public async Task<HandshakeReply> DoHandshakeAsync(DnsEndPoint endpoint, Handshake handshake)
    {
        var preCheckRes = await ValidateHandshakeAsync(endpoint, handshake);
        if (preCheckRes != null)
            return preCheckRes;

        if (handshake.HandshakeData.NodeVersion.GreaterThanSupportStreamMinVersion(NetworkOptions.SupportStreamMinVersion))
        {
            Logger.LogDebug("receive nodeversion>={version} handshake, will upgrade to stream, remote={endpoint}", KernelConstants.SupportStreamMinVersion, endpoint.Host);
            return new HandshakeReply { Handshake = await _handshakeProvider.GetHandshakeAsync(), Error = HandshakeError.HandshakeOk };
        }

        var pubkey = handshake.HandshakeData.Pubkey.ToHex();
        try
        {
            // mark the (IP; pubkey) pair as currently handshaking
            if (!_peerPool.AddHandshakingPeer(endpoint.Host, pubkey))
                return new HandshakeReply { Error = HandshakeError.ConnectionRefused };

            // create the connection to the peer
            var peerEndpoint = new AElfPeerEndpoint(endpoint.Host, handshake.HandshakeData.ListeningPort);
            var grpcPeer = await _peerDialer.DialBackPeerAsync(peerEndpoint, handshake);

            if (grpcPeer == null)
            {
                Logger.LogWarning($"Could not dial back {peerEndpoint}.");
                return new HandshakeReply { Error = HandshakeError.InvalidConnection };
            }

            var oldPeer = _peerPool.FindPeerByPublicKey(pubkey);
            if (oldPeer != null)
            {
                var oldPeerIsStream = oldPeer is GrpcStreamBackPeer;
                if (oldPeerIsStream && oldPeer.IsInvalid && _peerPool.TryReplace(pubkey, oldPeer, grpcPeer))
                {
                    await oldPeer.DisconnectAsync(false);
                    Logger.LogDebug("replace connection, oldPeerIsP2P={oldPeerIsStream} IsInvalid={IsInvalid} {pubkey}.", oldPeerIsStream, oldPeer.IsInvalid, grpcPeer.Info.Pubkey);
                }
                else
                {
                    Logger.LogDebug("Stopping connection, peer already in the pool oldPeerIsP2P={oldPeerIsStream} IsInvalid={IsInvalid} {pubkey}.", oldPeerIsStream, oldPeer.IsInvalid, grpcPeer.Info.Pubkey);
                    await grpcPeer.DisconnectAsync(false);
                    return new HandshakeReply { Error = HandshakeError.RepeatedConnection };
                }
            }
            // add the new peer to the pool
            else if (!_peerPool.TryAddPeer(grpcPeer))
            {
                Logger.LogDebug($"Stopping connection, peer already in the pool {grpcPeer.Info.Pubkey}.");
                await grpcPeer.DisconnectAsync(false);
                return new HandshakeReply { Error = HandshakeError.RepeatedConnection };
            }

            Logger.LogDebug($"Added to pool {grpcPeer.RemoteEndpoint} - {grpcPeer.Info.Pubkey}.");

            // send back our handshake
            var replyHandshake = await _handshakeProvider.GetHandshakeAsync();
            grpcPeer.InboundSessionId = replyHandshake.SessionId.ToByteArray();
            grpcPeer.UpdateLastSentHandshake(replyHandshake);

            Logger.LogDebug($"Sending back handshake to {peerEndpoint}.");
            return new HandshakeReply { Handshake = replyHandshake, Error = HandshakeError.HandshakeOk };
        }
        finally
        {
            // remove the handshaking mark (IP; pubkey)
            _peerPool.RemoveHandshakingPeer(endpoint.Host, pubkey);
        }
    }

    public async Task<HandshakeReply> DoHandshakeByStreamAsync(DnsEndPoint endpoint, IAsyncStreamWriter<StreamMessage> responseStream, Handshake handshake)
    {
        var preCheckRes = await ValidateHandshakeAsync(endpoint, handshake);
        if (preCheckRes != null)
            return preCheckRes;

        var pubkey = handshake.HandshakeData.Pubkey.ToHex();
        try
        {
            // mark the (IP; pubkey) pair as currently handshaking
            if (!_peerPool.AddHandshakingPeer(endpoint.Host, pubkey))
                return new HandshakeReply { Error = HandshakeError.ConnectionRefused };

            // create the connection to the peer
            var grpcPeer = await _peerDialer.DialBackPeerByStreamAsync(new AElfPeerEndpoint(endpoint.Host, handshake.HandshakeData.ListeningPort), responseStream, handshake);

            if (grpcPeer == null)
            {
                Logger.LogWarning("Could not dial back to stream {pubkey}.", pubkey);
                return new HandshakeReply { Error = HandshakeError.InvalidConnection };
            }

            var oldPeer = _peerPool.FindPeerByPublicKey(pubkey);
            if (oldPeer != null)
            {
                var oldPeerIsStream = oldPeer is GrpcStreamBackPeer;
                if (_peerPool.TryReplace(pubkey, oldPeer, grpcPeer))
                {
                    await oldPeer.DisconnectAsync(false);
                    Logger.LogDebug("replace connection, oldPeerIsP2P={oldPeerIsStream} IsInvalid={IsInvalid} {pubkey}.", oldPeerIsStream, oldPeer.IsInvalid, grpcPeer.Info.Pubkey);
                }
                else
                {
                    Logger.LogDebug("Stopping connection, peer already in the pool oldPeerIsP2P={oldPeerIsStream} IsInvalid={IsInvalid} {pubkey}.", oldPeerIsStream, oldPeer.IsInvalid, grpcPeer.Info.Pubkey);
                    await grpcPeer.DisconnectAsync(false);
                    return new HandshakeReply { Error = HandshakeError.RepeatedConnection };
                }
            }
            else if (!_peerPool.TryAddPeer(grpcPeer)) // add the new peer to the pool
            {
                Logger.LogDebug("Stopping connection, peer already in the pool {pubkey}.", grpcPeer.Info.Pubkey);
                await grpcPeer.DisconnectAsync(false);
                return new HandshakeReply { Error = HandshakeError.RepeatedConnection };
            }

            Logger.LogDebug($"Added to pool {grpcPeer.RemoteEndpoint} - {grpcPeer.Info.Pubkey}.");

            // send back our handshake
            var replyHandshake = await _handshakeProvider.GetHandshakeAsync();
            grpcPeer.InboundSessionId = replyHandshake.SessionId.ToByteArray();
            grpcPeer.UpdateLastSentHandshake(replyHandshake);

            Logger.LogDebug("Sending back handshake to {pubkey}.", pubkey);
            return new HandshakeReply { Handshake = replyHandshake, Error = HandshakeError.HandshakeOk };
        }
        finally
        {
            _peerPool.RemoveHandshakingPeer(endpoint.Host, pubkey);
        }
    }

    public void ConfirmHandshake(string peerPubkey)
    {
        var peer = _peerPool.FindPeerByPublicKey(peerPubkey) as GrpcPeer;
        if (peer == null)
        {
            Logger.LogWarning($"Cannot find Peer {peerPubkey} in the pool.");
            return;
        }

        Logger.LogInformation($"Connected to: {peer.RemoteEndpoint} - {peer.Info.Pubkey.Substring(0, 45)}" +
                              $" - in-token {peer.InboundSessionId?.ToHex()}, out-token {peer.OutboundSessionId?.ToHex()}" +
                              $" - LIB height {peer.LastKnownLibHeight}" +
                              $" - best chain [{peer.CurrentBlockHeight}, {peer.CurrentBlockHash}]");

        peer.IsConnected = true;
        peer.SyncState = SyncState.Syncing;

        FireConnectionEvent(peer);
    }

    public async Task DisconnectPeersAsync(bool gracefulDisconnect)
    {
        var peers = _peerPool.GetPeers(true);
        foreach (var peer in peers) await peer.DisconnectAsync(gracefulDisconnect);
    }

    public async Task RemovePeerAsync(string pubkey)
    {
        var peer = _peerPool.RemovePeer(pubkey);
        if (peer != null)
            await peer.DisconnectAsync(false);
    }

    public async Task<bool> BuildStreamForPeerAsync(IPeer peer)
    {
        if (peer is GrpcStreamPeer streamPeer) return await _peerDialer.BuildStreamForPeerAsync(streamPeer);
        return false;
    }

    public async Task<bool> CheckEndpointAvailableAsync(DnsEndPoint endpoint)
    {
        return await _peerDialer.CheckEndpointAvailableAsync(endpoint);
    }

    private void FireConnectionEvent(GrpcPeer peer)
    {
        var nodeInfo = new NodeInfo
            { Endpoint = peer.RemoteEndpoint.ToString(), Pubkey = ByteStringHelper.FromHexString(peer.Info.Pubkey) };
        var bestChainHash = peer.CurrentBlockHash;
        var bestChainHeight = peer.CurrentBlockHeight;

        _ = EventBus.PublishAsync(new PeerConnectedEventData(nodeInfo, bestChainHash, bestChainHeight));
    }

    private async Task<GrpcPeer> GetDialedPeerWithEndpointAsync(DnsEndPoint endpoint)
    {
        var peer = _peerPool.FindPeerByEndpoint(endpoint);
        if (peer != null)
        {
            if (peer.IsInvalid)
            {
                _peerPool.RemovePeer(peer.Info.Pubkey);
                await peer.DisconnectAsync(false);
            }
            else
            {
                Logger.LogDebug($"Peer with endpoint {endpoint} is already in the pool.");
                return null;
            }
        }

        if (_peerPool.IsPeerBlackListed(endpoint.Host))
        {
            Logger.LogDebug($"Peer with endpoint {endpoint} is blacklisted.");
            return null;
        }

        if (_peerPool.IsOverIpLimit(endpoint.Host))
        {
            Logger.LogDebug($"{endpoint.Host} is over ip limit.");
            return null;
        }

        var dialedPeer = await _peerDialer.DialPeerAsync(endpoint);

        if (dialedPeer == null)
        {
            Logger.LogDebug($"Error dialing {endpoint}.");
            return null;
        }

        return dialedPeer;
    }

    private HandshakeError GetHandshakeError(HandshakeValidationResult handshakeValidationResult)
    {
        HandshakeError handshakeError;

        switch (handshakeValidationResult)
        {
            case HandshakeValidationResult.InvalidChainId:
                handshakeError = HandshakeError.ChainMismatch;
                break;
            case HandshakeValidationResult.InvalidVersion:
                handshakeError = HandshakeError.ProtocolMismatch;
                break;
            case HandshakeValidationResult.HandshakeTimeout:
                handshakeError = HandshakeError.SignatureTimeout;
                break;
            case HandshakeValidationResult.InvalidSignature:
                handshakeError = HandshakeError.WrongSignature;
                break;
            case HandshakeValidationResult.Unauthorized:
            case HandshakeValidationResult.SelfConnection:
                handshakeError = HandshakeError.ConnectionRefused;
                break;
            default:
                throw new ArgumentException(
                    $"Unable to process handshake validation result: {handshakeValidationResult}");
        }

        return handshakeError;
    }
}