using System.Collections.Generic;
using AElf.Kernel.Account.Application;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Volo.Abp.Threading;

namespace AElf.OS.Network.Grpc
{
    public interface IPeerClientFactory
    {
        (Channel, PeerService.PeerServiceClient) CreateClientAsync(string ipAddress);
    }
    
    public class PeerClientFactory : IPeerClientFactory
    {
        private readonly IAccountService _accountService;

        public PeerClientFactory(IAccountService accountService)
        {
            _accountService = accountService;
        }
        
        public (Channel, PeerService.PeerServiceClient) CreateClientAsync(string ipAddress)
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

            return (channel, client);
        }
    }
}