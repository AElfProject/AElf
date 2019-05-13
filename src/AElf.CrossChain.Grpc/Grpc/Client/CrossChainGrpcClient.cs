using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.CrossChain;
using AElf.CrossChain.Cache;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace AElf.CrossChain.Grpc
{
    public abstract class CrossChainGrpcClient
    {
        protected readonly Channel Channel;
        protected readonly int DialTimeout;
        private readonly int _localChainId;
        public string Target => Channel.Target;
        private readonly CrossChainRpc.CrossChainRpcClient _grpcClient;

        protected CrossChainGrpcClient(string uri, int localChainId, int dialTimeout)
        {
            _localChainId = localChainId;
            DialTimeout = dialTimeout;
            Channel = CreateChannel(uri);
            _grpcClient = new CrossChainRpc.CrossChainRpcClient(Channel);
        }
        
        /// <summary>
        /// Create a new channel
        /// </summary>
        /// <param name="uriStr"></param>
        /// <param name="crt">Certificate</param>
        /// <returns></returns>
        private Channel CreateChannel(string uriStr, string crt)
        {
            var channelCredentials = new SslCredentials(crt);
            var channel = new Channel(uriStr, channelCredentials);
            return channel;
        }

        private Channel CreateChannel(string uriStr)
        {
            return new Channel(uriStr, ChannelCredentials.Insecure);
        }
        
        public async Task Close()
        {
            await Channel.ShutdownAsync();
        }
        
        public async Task StartIndexingRequest(int chainId, long targetHeight,
            IBlockCacheEntityProducer blockCacheEntityProducer, int localListeningPort)
        {
            var requestData = new CrossChainRequest
            {
                FromChainId = _localChainId,
                NextHeight = targetHeight,
                ListeningPort = localListeningPort
            };

            using (var serverStream = RequestIndexing(requestData))
            {
                while (await serverStream.ResponseStream.MoveNext())
                {
                    var response = serverStream.ResponseStream.Current;

                    // requestCrossChain failed or useless response
                    if (!blockCacheEntityProducer.TryAddBlockCacheEntity(new BlockCacheEntity {ChainId = response.BlockData.ChainId, Height = response.BlockData.Height, Payload = response.BlockData.Payload}))
                    {
                        break;
                    }

                    blockCacheEntityProducer.Logger.LogTrace(
                        $"Received response from chain {ChainHelpers.ConvertChainIdToBase58(response.BlockData.ChainId)} at height {response.BlockData.Height}");
                }
            }
        }
    
        public Task<HandShakeReply> HandShakeAsync(int chainId, int localListeningPort)
        {
            var handShakeReply = _grpcClient.CrossChainIndexingShakeAsync(new HandShake
            {
                FromChainId = _localChainId,
                ListeningPort = localListeningPort
            }, new CallOptions().WithDeadline(DateTime.UtcNow.AddSeconds(DialTimeout)));
            return Task.FromResult(handShakeReply);
        }
        
        public Task<SideChainInitializationInformation> RequestChainInitializationContext(int chainId)
        {
            var sideChainInitializationResponse = _grpcClient.RequestChainInitializationContextFromParentChainAsync(
                new SideChainInitializationRequest
                {
                    ChainId = chainId
                }, new CallOptions().WithDeadline(DateTime.UtcNow.AddSeconds(DialTimeout)));
            return Task.FromResult(sideChainInitializationResponse);
        }

        protected abstract AsyncServerStreamingCall<CrossChainResponse> RequestIndexing(
            CrossChainRequest crossChainRequest);
    }
    
    public class GrpcClientForSideChain : CrossChainGrpcClient
    {
        public GrpcClientForSideChain(string uri, int localChainId, int dialTimeout) : base(uri, localChainId, dialTimeout)
        {
        }

        protected override AsyncServerStreamingCall<CrossChainResponse> RequestIndexing(CrossChainRequest crossChainRequest)
        {
            return new CrossChainRpc.CrossChainRpcClient(Channel).RequestIndexingFromSideChainAsync(crossChainRequest,
                new CallOptions().WithDeadline(DateTime.UtcNow.AddSeconds(DialTimeout)));
        }
    }
    
    public class GrpcClientForParentChain : CrossChainGrpcClient
    {
        public GrpcClientForParentChain(string uri, int localChainId, int dialTimeout) : base(uri, localChainId, dialTimeout)
        {
        }

        protected override AsyncServerStreamingCall<CrossChainResponse> RequestIndexing(CrossChainRequest crossChainRequest)
        {
            return new CrossChainRpc.CrossChainRpcClient(Channel).RequestIndexingFromParentChainAsync(crossChainRequest,
                new CallOptions().WithDeadline(DateTime.UtcNow.AddSeconds(DialTimeout)));
        }
    }
}