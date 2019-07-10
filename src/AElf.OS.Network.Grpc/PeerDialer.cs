using System;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.Network.Infrastructure;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace AElf.OS.Network.Grpc
{
    public class PeerDialer : IPeerDialer
    {
        private NetworkOptions NetworkOptions => NetworkOptionsSnapshot.Value;
        public IOptionsSnapshot<NetworkOptions> NetworkOptionsSnapshot { get; set; }
        
        private readonly IPeerClientFactory _peerClientFactory;
        private readonly IHandshakeProvider _handshakeProvider;

        public PeerDialer(IPeerClientFactory peerClientFactory, IHandshakeProvider handshakeProvider)
        {
            _peerClientFactory = peerClientFactory;
            _handshakeProvider = handshakeProvider;
        }

        public async Task<GrpcPeer> DialPeerAsync(string ipAddress)
        {
            var (channel, client) = _peerClientFactory.CreateClientAsync(ipAddress);

            var handshake = await _handshakeProvider.GetHandshakeAsync();
            // TODO maybe implement retry logic
            ConnectReply connectReply = await ConnectAsync(client, channel, ipAddress, handshake);

            var pubKey = connectReply.Handshake.HandshakeData.Pubkey.ToHex();

            var connectionInfo = new PeerInfo
            {
                Pubkey = pubKey,
                ProtocolVersion = connectReply.Handshake.HandshakeData.Version,
                ConnectionTime = TimestampHelper.GetUtcNow().Seconds,
                StartHeight = connectReply.Handshake.BestChainBlockHeader.Height,
                LibHeightAtHandshake = connectReply.Handshake.LibBlockHeight
            };

            return new GrpcPeer(channel, client, handshake, ipAddress, connectionInfo);
        }
        
        private async Task<ConnectReply> ConnectAsync(PeerService.PeerServiceClient client, Channel channel, 
            string ipAddress, Handshake handshake)
        {
            ConnectReply connectReply = null;
            
            try
            {
                var metadata = new Metadata {
                    {GrpcConstants.TimeoutMetadataKey, NetworkOptions.PeerDialTimeoutInMilliSeconds.ToString()}};
                
                connectReply = await client.ConnectAsync(handshake, metadata);
            }
            catch (AggregateException ex)
            {
                await ExceptionHelpers.CleanupAndThrowAsync($"Could not connect to {ipAddress}.", channel, ex);
            }
            
            if (connectReply?.Handshake?.HandshakeData == null || connectReply.Error != AuthError.None)
                await ExceptionHelpers.CleanupAndThrowAsync($"Connect error: {connectReply?.Error}.", channel);
            
            return connectReply;
        }
    }
}