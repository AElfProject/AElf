using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.OS.Network.Infrastructure;
using Grpc.Core;

namespace AElf.OS.Network.Grpc
{
    public class GrpcPeer : IPeer
    {
        public event EventHandler DisconnectionEvent;
        
        private readonly Channel _channel;
        private readonly PeerService.PeerServiceClient _client;
        private readonly HandshakeData _handshakeData;

        /// <summary>
        /// Property that describes a valid state. Valid here means that the peer is ready to be used for communication.
        /// </summary>
        public bool IsReady
        {
            get { return _channel.State == ChannelState.Idle || _channel.State == ChannelState.Ready; }
        }
        
        public Hash CurrentBlockHash { get; set; }
        public long CurrentBlockHeight { get; set; }
        public string PeerAddress { get; }
        public string RemoteEndpoint { get; }

        private byte[] _pubKey;
        public byte[] PublicKey
        {
            get { return _pubKey ?? (_pubKey = _handshakeData?.PublicKey?.ToByteArray()); }
        }

        public GrpcPeer(Channel channel, PeerService.PeerServiceClient client, HandshakeData handshakeData,
            string peerAddress, string remoteEndpoint)
        {
            _channel = channel;
            _client = client;
            _handshakeData = handshakeData;

            RemoteEndpoint = remoteEndpoint;
            PeerAddress = peerAddress;
        }

        public async Task<Block> RequestBlockAsync(Hash hash)
        {
            try
            {
                BlockRequest request = new BlockRequest {Hash = hash};
                var blockReply = await _client.RequestBlockAsync(request);
                return blockReply?.Block;
            }
            catch (RpcException e)
            {
                HandleFailure(e);
            }

            return null;
        }

        public async Task<List<Block>> GetBlocksAsync(Hash firstHash, int count)
        {
            try
            {
                var list = await _client.RequestBlocksAsync(new BlocksRequest {PreviousBlockHash = firstHash, Count = count});

                if (list == null)
                    return new List<Block>();

                return list.Blocks.Select(b => b).ToList();
            }
            catch (RpcException e)
            {
                HandleFailure(e);
            }

            return new List<Block>();
        }

        public async Task AnnounceAsync(PeerNewBlockAnnouncement header)
        {
            try
            {
                await _client.AnnounceAsync(header);
            }
            catch (RpcException e)
            {
                HandleFailure(e);
            }
        }

        public async Task SendTransactionAsync(Transaction tx)
        {
            try
            {
                await _client.SendTransactionAsync(tx);
            }
            catch (RpcException e)
            {
                HandleFailure(e);
            }
        }

        /// <summary>
        /// This method handles the case where the peer is potentially down. If the Rpc call
        /// put the channel in TransientFailure or Connecting, we give the connection a certain time to recover.
        /// </summary>
        private void HandleFailure(RpcException rpcException)
        {
            // If channel has been shutdown (unrecoverable state) remove it.
            if (_channel.State == ChannelState.Shutdown)
            {
                DisconnectionEvent?.Invoke(this, EventArgs.Empty);
                return;
            }
                
            if (_channel.State == ChannelState.TransientFailure || _channel.State == ChannelState.Connecting)
            {
                Task.Run(async () =>
                {
                    await _channel.TryWaitForStateChangedAsync(_channel.State, DateTime.UtcNow.AddSeconds(NetworkConsts.DefaultPeerDialTimeout));

                    // Either we connected again or the state change wait timed out.
                    if (_channel.State == ChannelState.TransientFailure || _channel.State == ChannelState.Connecting)
                    {
                        try
                        {
                            await StopAsync(); // shutdown for good
                        }
                        catch (Exception e)
                        {
                            // no matter what happens here, we need to make sure the 
                            // DisconnectionEvent is fired.
                        }
                    
                        DisconnectionEvent?.Invoke(this, EventArgs.Empty);
                    }
                });
            }
            else
            {
                throw rpcException;
            }
        }

        public async Task StopAsync()
        {
            await _channel.ShutdownAsync();
        }

        public async Task SendDisconnectAsync()
        {
            await _client.DisconnectAsync(new DisconnectReason { Why = DisconnectReason.Types.Reason.Shutdown });
        }
    }
}