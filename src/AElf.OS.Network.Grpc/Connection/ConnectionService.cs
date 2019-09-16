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
                Logger.LogWarning($"Peer {endpoint} is already in the pool.");
                return false;
            }

            var peer = await _peerDialer.DialPeerAsync(endpoint);

            if (peer == null)
                return false;

            if (!_peerPool.TryAddPeer(peer))
            {
                Logger.LogWarning($"Peer {peer.Info.Pubkey} is already in the pool.");
                await peer.DisconnectAsync(false);
                return false;
            }

            Logger.LogDebug($"Added to pool {endpoint} - {peer.Info.Pubkey}.");

            try
            {
                await peer.ConfirmHandshakeAsync();
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Confirm handshake error. Peer: {peer.Info.Pubkey}.");
                _peerPool.RemovePeer(peer.Info.Pubkey);
                await peer.DisconnectAsync(false);
                throw;
            }

            peer.IsConnected = true;
            
            Logger.LogWarning($"Connected to: {peer.RemoteEndpoint} - {peer.Info.Pubkey.Substring(0, 45)}" +
                              $" - in-token {peer.InboundSessionId?.ToHex()}, out-token {peer.OutboundSessionId?.ToHex()}" +
                              $" - LIB height {peer.LastKnownLibHeight}" +
                              $" - best chain [{peer.CurrentBlockHeight}, {peer.CurrentBlockHash}]");

            FireConnectionEvent(peer);

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

                Logger.LogDebug($"Added to pool {endpoint} - {grpcPeer.Info.Pubkey}.");

                // send back our handshake
                var replyHandshake = await _handshakeProvider.GetHandshakeAsync();
                grpcPeer.InboundSessionId = replyHandshake.SessionId.ToByteArray();

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