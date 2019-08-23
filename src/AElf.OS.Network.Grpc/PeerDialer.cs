using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AElf.Kernel.Account.Application;
using AElf.OS.Network.Grpc.Extensions;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Options;
using Volo.Abp.Threading;

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
        
        private readonly IConnectionInfoProvider _connectionInfoProvider;
        private readonly IAccountService _accountService;

        public PeerDialer(IConnectionInfoProvider connectionInfoProvider, IAccountService accountService)
        {
            _connectionInfoProvider = connectionInfoProvider;
            _accountService = accountService;
        }

        /// <summary>
        /// Given an IP address, will create a connection to the distant node for
        /// further communications.
        /// </summary>
        /// <returns>The created peer</returns>
        public async Task<GrpcPeer> DialPeerAsync(IPEndPoint endpoint)
        {
            var client = CreateClient(endpoint);
            var connectionInfo = await _connectionInfoProvider.GetConnectionInfoAsync();
            
            ConnectReply connectReply = await CallConnectAsync(client, endpoint, connectionInfo);

            if (connectReply?.Info?.Pubkey == null || connectReply.Error != ConnectError.ConnectOk)
            {
                await CleanupAndGetExceptionAsync($"Connect error: {connectReply?.Error}.", client.Channel);
            }
            
            return new GrpcPeer(client, endpoint, connectReply.Info.ToPeerInfo(isInbound: false));
        }

        /// <summary>
        /// Calls the server side connect RPC method, in order to establish a 2-way connection.
        /// </summary>
        /// <returns>The reply from the server.</returns>
        private async Task<ConnectReply> CallConnectAsync(GrpcClient client, IPEndPoint ipAddress, 
            ConnectionInfo connectionInfo)
        {
            ConnectReply connectReply = null;
            
            try
            {
                var metadata = new Metadata {
                    {GrpcConstants.TimeoutMetadataKey, (NetworkOptions.PeerDialTimeoutInMilliSeconds*2).ToString()}};
                
                connectReply = await client.Client.ConnectAsync(new ConnectRequest { Info = connectionInfo }, metadata);
            }
            catch (AggregateException ex)
            {
                await CleanupAndGetExceptionAsync($"Could not connect to {ipAddress}.", client.Channel, ex);
            }
            
            return connectReply;
        }
        
        public async Task<GrpcPeer> DialBackPeer(IPEndPoint endpoint, ConnectionInfo peerConnectionInfo)
        {
            var client = CreateClient(endpoint);
            
            await PingNodeAsync(client, endpoint);
            return new GrpcPeer(client, endpoint, peerConnectionInfo.ToPeerInfo(isInbound: true));
        }
        
        /// <summary>
        /// Checks that the distant node is reachable by pinging it.
        /// </summary>
        /// <returns>The reply from the server.</returns>
        private async Task PingNodeAsync(GrpcClient client, IPEndPoint ipAddress)
        {
            try
            {
                var metadata = new Metadata {
                    {GrpcConstants.TimeoutMetadataKey, NetworkOptions.PeerDialTimeoutInMilliSeconds.ToString()}};
                
                await client.Client.PingAsync(new PingRequest(), metadata);
            }
            catch (AggregateException ex)
            {
                await CleanupAndGetExceptionAsync($"Could not ping {ipAddress}.", client.Channel, ex);
            }
        }
        
        private async Task CleanupAndGetExceptionAsync(string exceptionMessage, Channel channel, Exception inner = null)
        {
            await channel.ShutdownAsync();
            throw new PeerDialException(exceptionMessage, inner);
        }
        
        /// <summary>
        /// Creates a channel/client pair with the appropriate options and interceptors.
        /// </summary>
        /// <returns>A tuple of the channel and client</returns>
        public GrpcClient CreateClient(IPEndPoint endpoint)
        {
            var channel = new Channel(endpoint.ToString(), ChannelCredentials.Insecure, new List<ChannelOption>
            {
                new ChannelOption(ChannelOptions.MaxSendMessageLength, GrpcConstants.DefaultMaxSendMessageLength),
                new ChannelOption(ChannelOptions.MaxReceiveMessageLength, GrpcConstants.DefaultMaxReceiveMessageLength)
            });
            
            var nodePubkey = AsyncHelper.RunSync(() => _accountService.GetPublicKeyAsync()).ToHex();
            
            var interceptedChannel = channel.Intercept(metadata =>
            {
                metadata.Add(GrpcConstants.PubkeyMetadataKey, nodePubkey);
                return metadata;
            }).Intercept(new RetryInterceptor());

            var client = new PeerService.PeerServiceClient(interceptedChannel);

            return new GrpcClient(channel, client);
        }
    }
}