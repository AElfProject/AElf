using System;
using System.Net;
using System.Threading.Tasks;
using AElf.OS.Network.Events;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Network.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.EventBus.Local;

namespace AElf.OS.Network.Grpc.Connection
{
    public class ConnectionService : IConnectionService
    {
        private readonly IPeerPool _peerPool;
        private readonly IPeerDialer _peerDialer;
        private readonly IHandshakeProvider _handshakeProvider;
        public ILocalEventBus EventBus { get; set; }
        public ILogger<ConnectionService> Logger { get; set; }

        public ConnectionService(IPeerPool peerPool, IPeerDialer peerDialer,
            IHandshakeProvider handshakeProvider)
        {
            _peerPool = peerPool;
            _peerDialer = peerDialer;
            _handshakeProvider = handshakeProvider;

            Logger = NullLogger<ConnectionService>.Instance;
            EventBus = NullLocalEventBus.Instance;
        }

        public async Task DisconnectAsync(IPeer peer, bool sendDisconnect = false)
        {
            if (peer == null)
                throw new ArgumentNullException(nameof(peer));

            // clean the pool
            if (_peerPool.RemovePeer(peer.Info.Pubkey) == null)
                Logger.LogWarning($"{peer} was not found in pool.");

            // clean the peer
            await peer.DisconnectAsync(sendDisconnect);

            Logger.LogDebug($"Removed peer {peer}");
        }

        public GrpcPeer GetPeerByPubkey(string pubkey)
        {
            return _peerPool.FindPeerByPublicKey(pubkey) as GrpcPeer;
        }

        /// <summary>
        /// Connects to a node with the given ip address and adds it to the node's peer pool.
        /// </summary>
        /// <param name="endpoint">the ip address of the distant node</param>
        /// <returns>True if the connection was successful, false otherwise</returns>
        public async Task<bool> ConnectAsync(IPEndPoint endpoint)
        {
            Logger.LogTrace($"Attempting to reach {endpoint}.");

            if (_peerPool.FindPeerByEndpoint(endpoint) != null)
            {
                Logger.LogWarning($"Peer with endpoint {endpoint} is already in the pool.");
                return false;
            }

            var dialedPeer = await _peerDialer.DialPeerAsync(endpoint);

            if (dialedPeer == null)
            {
                Logger.LogWarning($"Error dialing {endpoint}.");
                return false;
            }

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
            
            GrpcPeer currentPeer = dialedPeer;
            if (inboundPeer != null)
            {
                Logger.LogWarning("Duplicate peer connection detected: " +
                                  $"{inboundPeer} ({inboundPeer.LastReceivedHandshakeTime}) " +
                                  $"vs {dialedPeer} ({dialedPeer.LastSentHandshakeTime}).");

                if (inboundPeer.LastReceivedHandshakeTime > dialedPeer.LastSentHandshakeTime)
                {
                    // we started the dial first, replace the inbound connection with the dialed 
                    if (!_peerPool.TryReplace(inboundPeer.Info.Pubkey, inboundPeer, dialedPeer))
                        Logger.LogWarning("Replacing the inbound connection failed.");
                    
                    await inboundPeer.DisconnectAsync(false);

                    Logger.LogWarning($"Replaced the inbound connection with the dialed peer {inboundPeer} .");
                }
                else
                {
                    // keep the inbound connection
                    await dialedPeer.DisconnectAsync(false);
                    currentPeer = inboundPeer;
                    
                    Logger.LogWarning($"Disconnected dialed peer {dialedPeer}.");
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
                Logger.LogError(e, $"Confirm handshake error. Peer: {currentPeer.Info.Pubkey}.");
                _peerPool.RemovePeer(currentPeer.Info.Pubkey);
                await currentPeer.DisconnectAsync(false);
                throw;
            }

            currentPeer.IsConnected = true;
            
            Logger.LogWarning($"Connected to: {currentPeer.RemoteEndpoint} - {currentPeer.Info.Pubkey.Substring(0, 45)}" +
                              $" - in-token {currentPeer.InboundSessionId?.ToHex()}, out-token {currentPeer.OutboundSessionId?.ToHex()}" +
                              $" - LIB height {currentPeer.LastKnownLibHeight}" +
                              $" - best chain [{currentPeer.CurrentBlockHeight}, {currentPeer.CurrentBlockHash}]");

            FireConnectionEvent(currentPeer);

            return true;
        }

        private void FireConnectionEvent(GrpcPeer peer)
        {
            var nodeInfo = new NodeInfo {Endpoint = peer.RemoteEndpoint.ToString(), Pubkey = peer.Info.Pubkey.ToByteString()};
            var bestChainHash = peer.CurrentBlockHash;
            var bestChainHeight = peer.CurrentBlockHeight;

            _ = EventBus.PublishAsync(new PeerConnectedEventData(nodeInfo, bestChainHash, bestChainHeight));
        }

        public async Task<HandshakeReply> DoHandshakeAsync(IPEndPoint endpoint, Handshake handshake)
        {
            // validate the handshake (signature, chain id...)
            var handshakeValidationResult = await _handshakeProvider.ValidateHandshakeAsync(handshake);
            if (handshakeValidationResult != HandshakeValidationResult.Ok)
            {
                var handshakeError = GetHandshakeError(handshakeValidationResult);
                return new HandshakeReply {Error = handshakeError};
            }

            var pubkey = handshake.HandshakeData.Pubkey.ToHex();
            
            // remove any remaining connection to the peer (before the check
            // that we have room for more connections)
            var currentPeer = _peerPool.FindPeerByPublicKey(pubkey);
            if (currentPeer != null)
            {
                _peerPool.RemovePeer(pubkey);
                await currentPeer.DisconnectAsync(false);
            }
            
            try
            {
                // mark the (IP; pubkey) pair as currently handshaking
                if (!_peerPool.AddHandshakingPeer(endpoint.Address, pubkey))
                    return new HandshakeReply {Error = HandshakeError.ConnectionRefused};

                // create the connection to the peer
                var peerAddress = new IPEndPoint(endpoint.Address, handshake.HandshakeData.ListeningPort);
                var grpcPeer = await _peerDialer.DialBackPeerAsync(peerAddress, handshake);

                // add the new peer to the pool
                if (!_peerPool.TryAddPeer(grpcPeer))
                {
                    Logger.LogWarning($"Stopping connection, peer already in the pool {grpcPeer.Info.Pubkey}.");
                    await grpcPeer.DisconnectAsync(false);
                    return new HandshakeReply {Error = HandshakeError.RepeatedConnection};
                }

                Logger.LogDebug($"Added to pool {grpcPeer.RemoteEndpoint} - {grpcPeer.Info.Pubkey}.");

                // send back our handshake
                var replyHandshake = await _handshakeProvider.GetHandshakeAsync();
                grpcPeer.InboundSessionId = replyHandshake.SessionId.ToByteArray();
                grpcPeer.UpdateLastSentHandshake(replyHandshake);

                return new HandshakeReply { Handshake = replyHandshake, Error = HandshakeError.HandshakeOk };
            }
            finally
            {
                // remove the handshaking mark (IP; pubkey)
                _peerPool.RemoveHandshakingPeer(endpoint.Address, pubkey);
            }
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
                case HandshakeValidationResult.InvalidSignature:
                    handshakeError = HandshakeError.WrongSignature;
                    break;
                case HandshakeValidationResult.Unauthorized:
                case HandshakeValidationResult.SelfConnection:
                    handshakeError = HandshakeError.ConnectionRefused;
                    break;
                default:
                    throw new ArgumentException($"Unable to process handshake validation result: {handshakeValidationResult}");
            }

            return handshakeError;
        }

        public void ConfirmHandshake(string peerPubkey)
        {
            var peer = _peerPool.FindPeerByPublicKey(peerPubkey) as GrpcPeer;
            if (peer == null)
            {
                Logger.LogWarning($"Cannot find Peer {peerPubkey} in the pool.");
                return;
            }
            
            Logger.LogWarning($"Connected to: {peer.RemoteEndpoint} - {peer.Info.Pubkey.Substring(0, 45)}" +
                              $" - in-token {peer.InboundSessionId?.ToHex()}, out-token {peer.OutboundSessionId?.ToHex()}" +
                              $" - LIB height {peer.LastKnownLibHeight}" +
                              $" - best chain [{peer.CurrentBlockHeight}, {peer.CurrentBlockHash}]");

            peer.IsConnected = true;
            
            FireConnectionEvent(peer);
        }

        public async Task DisconnectPeersAsync(bool gracefulDisconnect)
        {
            var peers = _peerPool.GetPeers(true);
            foreach (var peer in peers)
            {
                await peer.DisconnectAsync(gracefulDisconnect);
            }
        }

        public void RemovePeer(string pubkey)
        {
            _peerPool.RemovePeer(pubkey);
        }
    }
}