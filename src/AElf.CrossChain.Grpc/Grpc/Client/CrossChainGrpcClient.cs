using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChain.Cache;
using AElf.Kernel;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace AElf.CrossChain.Grpc
{
    public abstract class CrossChainGrpcClient<TResponse> : CrossChainGrpcClient where TResponse : IResponseIndexingMessage
    {
        private readonly CrossChainRpc.CrossChainRpcClient _client;
        public override async Task StartIndexingRequest(int chainId, long targetHeight,
            ICrossChainDataProducer crossChainDataProducer)
        {
            var requestData = new RequestCrossChainBlockData
            {
                FromChainId = LocalChainId,
                NextHeight = targetHeight
            };

            using (var serverStream = RequestIndexing(requestData))
            {
                while (await serverStream.ResponseStream.MoveNext())
                {
                    var response = serverStream.ResponseStream.Current;

                    // requestCrossChain failed or useless response
                    if (!response.Success || !crossChainDataProducer.AddNewBlockInfo(response.BlockInfoResult))
                    {
                        break;
                    }

                    crossChainDataProducer.Logger.LogTrace(
                        $"Received response from chain {ChainHelpers.ConvertChainIdToBase58(response.BlockInfoResult.ChainId)} at height {response.Height}");
                }
            }
        }
    
        public override Task<IndexingHandShakeReply> HandShakeAsync(int chainId, int localListeningPort)
        {
            var handShakeReply = _client.CrossChainIndexingShake(new IndexingHandShake
            {
                ChainId = chainId,
                ListeningPort = localListeningPort
            }, new CallOptions().WithDeadline(DateTime.UtcNow.AddSeconds(DialTimeout)));
            return Task.FromResult(handShakeReply);
        }
        
        public override Task<ChainInitializationContext> RequestChainInitializationContext(int chainId)
        {
            var chainInitializationResponse = _client.RequestChainInitializationContextFromParentChain(
                new ChainInitializationRequest
                {
                    ChainId = chainId
                }, new CallOptions().WithDeadline(DateTime.UtcNow.AddSeconds(DialTimeout)));
            return Task.FromResult(chainInitializationResponse.SideChainInitializationContext);
        }

        //protected abstract AsyncDuplexStreamingCall<RequestCrossChainBlockData, TResponse> CallWithDuplexStreaming(int milliSeconds = 0);

        protected abstract AsyncServerStreamingCall<TResponse> RequestIndexing(
            RequestCrossChainBlockData requestCrossChainBlockData);

        protected CrossChainGrpcClient(string uri, int localChainId, int dialTimeout) : base(uri, localChainId, dialTimeout)
        {
            _client = new CrossChainRpc.CrossChainRpcClient(Channel);
        }
    }
    
    public class GrpcClientForSideChain : CrossChainGrpcClient<ResponseSideChainBlockData>
    {
        public GrpcClientForSideChain(string uri, int localChainId, int dialTimeout) : base(uri, localChainId, dialTimeout)
        {
        }

        protected override AsyncServerStreamingCall<ResponseSideChainBlockData> RequestIndexing(RequestCrossChainBlockData requestCrossChainBlockData)
        {
            return new CrossChainRpc.CrossChainRpcClient(Channel).RequestIndexingFromSideChain(requestCrossChainBlockData,
                new CallOptions().WithDeadline(DateTime.UtcNow.AddSeconds(DialTimeout)));
        }
    }
    
    public class GrpcClientForParentChain : CrossChainGrpcClient<ResponseParentChainBlockData>
    {
        public GrpcClientForParentChain(string uri, int localChainId, int dialTimeout) : base(uri, localChainId, dialTimeout)
        {
        }

        protected override AsyncServerStreamingCall<ResponseParentChainBlockData> RequestIndexing(RequestCrossChainBlockData requestCrossChainBlockData)
        {
            return new CrossChainRpc.CrossChainRpcClient(Channel).RequestIndexingFromParentChain(requestCrossChainBlockData,
                new CallOptions().WithDeadline(DateTime.UtcNow.AddSeconds(DialTimeout)));
        }
    }
    
    public abstract class CrossChainGrpcClient
    {
        protected readonly Channel Channel;
        protected readonly int LocalChainId;
        protected readonly int DialTimeout;

        protected CrossChainGrpcClient(string uri, int localChainId, int dialTimeout)
        {
            LocalChainId = localChainId;
            DialTimeout = dialTimeout;
            Channel = CreateChannel(uri);
        }
        
        public abstract Task<IndexingHandShakeReply> HandShakeAsync(int chainId, int localListeningPort);
        public abstract Task StartIndexingRequest(int chainId, long targetHeight, ICrossChainDataProducer crossChainDataProducer);
        public abstract Task<ChainInitializationContext> RequestChainInitializationContext(int chainId);

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
    }
}