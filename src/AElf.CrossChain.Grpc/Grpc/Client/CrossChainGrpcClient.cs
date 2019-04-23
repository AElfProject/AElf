using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChain.Cache;
using AElf.Kernel;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace AElf.CrossChain.Grpc
{
    public abstract class CrossChainGrpcClient<TResponse> : IGrpcCrossChainClient where TResponse : IResponseIndexingMessage
    {
        protected CrossChainRpc.CrossChainRpcClient Client;
        protected int LocalChainId;
        protected int DialTimeout;
        
        public async Task<bool> StartIndexingRequest(int chainId, long targetHeight,
            ICrossChainDataProducer crossChainDataProducer)
        {
            var requestData = new RequestCrossChainBlockData
            {
                FromChainId = LocalChainId,
                NextHeight = targetHeight
            };

            var serverStream = RequestIndexing(requestData);
            await ReadResponse(serverStream, crossChainDataProducer);
            return true;
        }

        private Task ReadResponse(AsyncServerStreamingCall<TResponse> serverStream, ICrossChainDataProducer crossChainDataProducer)
        {
            var responseReaderTask = Task.Run(async () =>
            {
                while (await serverStream.ResponseStream.MoveNext())
                {
                    var response = serverStream.ResponseStream.Current;

                    // requestCrossChain failed or useless response
                    if (!response.Success)
                    {
                        continue;
                    }
                    if(!crossChainDataProducer.AddNewBlockInfo(response.BlockInfoResult))
                        continue;
                    crossChainDataProducer.Logger.LogTrace(
                        $"Received response from chain {ChainHelpers.ConvertChainIdToBase58(response.BlockInfoResult.ChainId)} at height {response.Height}");
                }
            });
    
            return responseReaderTask;
        }

        public Task<IndexingHandShakeReply> TryHandShakeAsync(int chainId, int localListeningPort)
        {
            var handShakeReply = Client.CrossChainIndexingShake(new IndexingHandShake
            {
                ChainId = chainId,
                ListeningPort = localListeningPort
            }, new CallOptions().WithDeadline(DateTime.UtcNow.AddSeconds(DialTimeout)));
            return Task.FromResult(handShakeReply);
        }
        
        public Task<ChainInitializationResponse> RequestChainInitializationContext(int chainId)
        {
            var chainInitializationResponse = Client.RequestChainInitializationContextFromParentChain(
                new ChainInitializationRequest
                {
                    ChainId = chainId
                }, new CallOptions().WithDeadline(DateTime.UtcNow.AddSeconds(DialTimeout)));
            return Task.FromResult(chainInitializationResponse);
        }

        //protected abstract AsyncDuplexStreamingCall<RequestCrossChainBlockData, TResponse> CallWithDuplexStreaming(int milliSeconds = 0);

        protected abstract AsyncServerStreamingCall<TResponse> RequestIndexing(
            RequestCrossChainBlockData requestCrossChainBlockData);
        
        /// <summary>
        /// Create a new channel
        /// </summary>
        /// <param name="uriStr"></param>
        /// <param name="crt">Certificate</param>
        /// <returns></returns>
        protected Channel CreateChannel(string uriStr, string crt)
        {
            var channelCredentials = new SslCredentials(crt);
            var channel = new Channel(uriStr, channelCredentials);
            return channel;
        }

        protected Channel CreateChannel(string uriStr)
        {
            return new Channel(uriStr, ChannelCredentials.Insecure);
        }
    }
    
    public class GrpcClientForSideChain : CrossChainGrpcClient<ResponseSideChainBlockData>
    {

        public GrpcClientForSideChain(string uri, int dialTimeout)
        {
            DialTimeout = dialTimeout;
            Client = new CrossChainRpc.CrossChainRpcClient(CreateChannel(uri));
        }

        protected override AsyncServerStreamingCall<ResponseSideChainBlockData> RequestIndexing(RequestCrossChainBlockData requestCrossChainBlockData)
        {
            return Client.RequestIndexingFromSideChain(requestCrossChainBlockData,
                new CallOptions().WithDeadline(DateTime.UtcNow.AddSeconds(DialTimeout)));
        }
    }
    
    public class GrpcClientForParentChain : CrossChainGrpcClient<ResponseParentChainBlockData>
    {
        public GrpcClientForParentChain(string uri, int localChainId, int dialTimeout)
        {
            LocalChainId = localChainId;
            DialTimeout = dialTimeout;
            Client = new CrossChainRpc.CrossChainRpcClient(CreateChannel(uri));
        }

        protected override AsyncServerStreamingCall<ResponseParentChainBlockData> RequestIndexing(RequestCrossChainBlockData requestCrossChainBlockData)
        {
            return Client.RequestIndexingFromParentChain(requestCrossChainBlockData,
                new CallOptions().WithDeadline(DateTime.UtcNow.AddSeconds(DialTimeout)));
        }
    }

    public interface IGrpcCrossChainClient
    {
        //Task StartDuplexStreamingCall(int chainId, CancellationToken cancellationToken);
        Task<IndexingHandShakeReply> TryHandShakeAsync(int chainId, int localListeningPort);
        Task<bool> StartIndexingRequest(int chainId, long targetHeight,
            ICrossChainDataProducer crossChainDataProducer);
        Task<ChainInitializationResponse> RequestChainInitializationContext(int chainId);
    }
}