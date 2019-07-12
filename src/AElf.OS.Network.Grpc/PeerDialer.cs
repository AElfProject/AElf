using System;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace AElf.OS.Network.Grpc
{
    public class PeerDialer : IPeerDialer
    {
        private NetworkOptions NetworkOptions => NetworkOptionsSnapshot.Value;
        public IOptionsSnapshot<NetworkOptions> NetworkOptionsSnapshot { get; set; }
        
        private readonly IPeerClientFactory _peerClientFactory;
        private readonly IConnectionInfoProvider _connectionInfoProvider;


        public PeerDialer(IPeerClientFactory peerClientFactory, IConnectionInfoProvider connectionInfoProvider)
        {
            _peerClientFactory = peerClientFactory;
            _connectionInfoProvider = connectionInfoProvider;
        }

        public async Task<GrpcPeer> DialPeerAsync(string ipAddress)
        {
            var (channel, client) = _peerClientFactory.CreateClientAsync(ipAddress);
            var connectInfo = await _connectionInfoProvider.GetConnectionInfoAsync();
            
            // TODO maybe implement retry logic (for now in interception)
            ConnectReply connectReply = await ConnectAsync(client, channel, ipAddress, connectInfo);

            if (connectReply?.Info?.Pubkey == null || connectReply.Error != ConnectError.ConnectOk)
            {
                // TODO can check more here
                await ExceptionHelpers.CleanupAndThrowAsync($"Connect error: {connectReply?.Error}.", channel);
            }

            return new GrpcPeer(channel, client, ipAddress, connectReply.Info.ToPeerInfo(false));
        }
        
        private async Task<ConnectReply> ConnectAsync(PeerService.PeerServiceClient client, Channel channel, 
            string ipAddress, ConnectionInfo connectionInfo)
        {
            ConnectReply connectReply = null;
            
            try
            {
                var metadata = new Metadata {
                    {GrpcConstants.TimeoutMetadataKey, NetworkOptions.PeerDialTimeoutInMilliSeconds.ToString()}};
                
                connectReply = await client.ConnectAsync(new ConnectRequest { Info = connectionInfo }, metadata);
            }
            catch (AggregateException ex)
            {
                await ExceptionHelpers.CleanupAndThrowAsync($"Could not connect to {ipAddress}.", channel, ex);
            }
            
            return connectReply;
        }
    }
}