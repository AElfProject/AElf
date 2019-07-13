using System;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace AElf.OS.Network.Grpc
{
    /// <summary>
    /// Provides functionality to setup a connection to a distant node by exchanging some
    /// low level information.
    /// </summary>
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

        /// <summary>
        /// Given an IP address, will create a connection to the distant node for
        /// further communications.
        /// </summary>
        /// <returns>The created peer</returns>
        public async Task<GrpcPeer> DialPeerAsync(string ipAddress)
        {
            var (channel, client) = _peerClientFactory.CreateClientAsync(ipAddress);
            
            // TODO maybe implement retry logic (for now in interception)
            ConnectReply connectReply = await CallConnectAsync(client, channel, ipAddress);

            if (connectReply?.Info?.Pubkey == null || connectReply.Error != ConnectError.ConnectOk)
            {
                // TODO can check more here
                throw await ExceptionHelpers.CleanupAndGetExceptionAsync($"Connect error: {connectReply?.Error}.", channel);
            }

            return new GrpcPeer(channel, client, ipAddress, connectReply.Info.ToPeerInfo(false));
        }
        
        /// <summary>
        /// Calls the server side connect RPC method, in order to establish a 2-way connection.
        /// </summary>
        /// <returns>The reply from the server.</returns>
        private async Task<ConnectReply> CallConnectAsync(PeerService.PeerServiceClient client, Channel channel, 
            string ipAddress)
        {
            ConnectReply connectReply = null;
            
            try
            {
                var metadata = new Metadata {
                    {GrpcConstants.TimeoutMetadataKey, NetworkOptions.PeerDialTimeoutInMilliSeconds.ToString()}};
                
                var connectionInfo = await _connectionInfoProvider.GetConnectionInfoAsync();
                connectReply = await client.ConnectAsync(new ConnectRequest { Info = connectionInfo }, metadata);
            }
            catch (AggregateException ex)
            {
                throw await ExceptionHelpers.CleanupAndGetExceptionAsync($"Could not connect to {ipAddress}.", channel, ex);
            }
            
            return connectReply;
        }
    }
}