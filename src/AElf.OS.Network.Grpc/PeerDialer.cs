using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.OS.Network.Protocol;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        
        private readonly IAccountService _accountService;
        private readonly IHandshakeProvider _handshakeProvider;
        
        public ILogger<PeerDialer> Logger { get; set; }

        public PeerDialer(IAccountService accountService,
            IHandshakeProvider handshakeProvider)
        {
            _accountService = accountService;
            _handshakeProvider = handshakeProvider;
            
            Logger = NullLogger<PeerDialer>.Instance;
        }

        /// <summary>
        /// Given an IP address, will create a handshake to the distant node for
        /// further communications.
        /// </summary>
        /// <returns>The created peer</returns>
        public async Task<GrpcPeer> DialPeerAsync(string ipAddress)
        {
            var handshake = await _handshakeProvider.GetHandshakeAsync();
            var client = CreateClient(ipAddress);

            var handshakeReply = await CallDoHandshakeAsync(client, ipAddress, handshake);

            // verify handshake
            if (!await ValidateHandshakeAsync(handshakeReply.Handshake))
            {
                await CleanupAndGetExceptionAsync($"Connect error: {handshakeReply}.", client.Channel);
                return null;
            }

            var peer = new GrpcPeer(client, ipAddress, new PeerInfo
            {
                Pubkey = handshakeReply.Handshake.HandshakeData.Pubkey.ToHex(),
                ConnectionTime = TimestampHelper.GetUtcNow().Seconds,
                ProtocolVersion = handshakeReply.Handshake.HandshakeData.Version,
                IsInbound = false
            });
            
            peer.UpdateLastReceivedHandshake(handshakeReply.Handshake);
            peer.IsConnected = false;

            return peer;
        }

        /// <summary>
        /// Calls the server side DoHandshake RPC method, in order to establish a 2-way connection.
        /// </summary>
        /// <returns>The reply from the server.</returns>
        private async Task<HandshakeReply> CallDoHandshakeAsync(GrpcClient client, string ipAddress,
            Handshake handshake)
        {
            HandshakeReply handshakeReply = null;

            try
            {
                var metadata = new Metadata
                {
                    {GrpcConstants.TimeoutMetadataKey, (NetworkOptions.PeerDialTimeoutInMilliSeconds * 2).ToString()}
                };

                handshakeReply =
                    await client.Client.DoHandshakeAsync(new HandshakeRequest {Handshake = handshake}, metadata);
            }
            catch (AggregateException ex)
            {
                await CleanupAndGetExceptionAsync($"Could not connect to {ipAddress}.", client.Channel, ex);
            }

            return handshakeReply;
        }

        private async Task<bool> ValidateHandshakeAsync(Handshake handshake)
        {
            if (!await _handshakeProvider.ValidateHandshakeAsync(handshake))
            {
                return false;
            }

            // verify authentication
            var pubkey = handshake.HandshakeData.Pubkey.ToHex();

            if (NetworkOptions.AuthorizedPeers == AuthorizedPeers.Authorized &&
                !NetworkOptions.AuthorizedKeys.Contains(pubkey))
            {
                Logger.LogDebug($"{pubkey} not in the authorized peers.");
                return false;
            }

            return true;
        }

        public async Task<GrpcPeer> DialBackPeerAsync(string ipAddress, Handshake handshake)
        {
            if (!await ValidateHandshakeAsync(handshake))
            {
                return null;
            }
            
            var client = CreateClient(ipAddress);
            await PingNodeAsync(client, ipAddress);
            
            var peer = new GrpcPeer(client, ipAddress, new PeerInfo
            {
                Pubkey = handshake.HandshakeData.Pubkey.ToHex(),
                ConnectionTime = TimestampHelper.GetUtcNow().Seconds,
                ProtocolVersion = handshake.HandshakeData.Version,
                IsInbound = false
            });
            
            peer.UpdateLastReceivedHandshake(handshake);
            peer.IsConnected = false;

            return peer;
        }

        /// <summary>
        /// Checks that the distant node is reachable by pinging it.
        /// </summary>
        /// <returns>The reply from the server.</returns>
        private async Task PingNodeAsync(GrpcClient client, string ipAddress)
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
        public GrpcClient CreateClient(string ipAddress)
        {
            var channel = new Channel(ipAddress, ChannelCredentials.Insecure, new List<ChannelOption>
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