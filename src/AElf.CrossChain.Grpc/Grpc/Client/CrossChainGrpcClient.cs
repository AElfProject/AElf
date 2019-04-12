using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.CrossChain.Cache;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.CrossChain.Grpc
{
    public abstract class CrossChainGrpcClient<TResponse> : IGrpcCrossChainClient where TResponse : IResponseIndexingMessage
    {
        protected CrossChainRpc.CrossChainRpcClient Client;

        protected int LocalChainId;

        public async Task<bool> StartIndexingRequest(int chainId, ICrossChainDataProducer crossChainDataProducer)
        {
            var targetHeight = crossChainDataProducer.GetChainHeightNeeded(chainId);
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
                // use formatted chainId as certificate name, which can be changed later.  
            });
            return Task.FromResult(handShakeReply);
        }
        
        public Task<ChainInitializationResponse> RequestChainInitializationContext(int chainId)
        {
            var chainInitializationResponse = Client.RequestChainInitializationContextFromParentChain(
                new ChainInitializationRequest
                {
                    ChainId = chainId
                });
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

        public GrpcClientForSideChain(string uri)
        {
            Client = new CrossChainRpc.CrossChainRpcClient(CreateChannel(uri));
        }

//        protected override AsyncDuplexStreamingCall<RequestCrossChainBlockData, ResponseSideChainBlockData> CallWithDuplexStreaming(int milliSeconds = 0)
//        {
//            return milliSeconds == 0
//                ? _client.RequestSideChainDuplexStreaming()
//                : _client.RequestSideChainDuplexStreaming(deadline: DateTime.UtcNow.AddMilliseconds(milliSeconds));
//        }

//        public override Task<IndexingHandShakeReply> TryHandShakeAsync(int remoteChainId, int localListeningPort)
//        {
//            // dont handshake with side chain.
//            return await Client.CrossChainIndexingShakeAsync(new IndexingHandShake
//            {
//                SideChainId = remoteChainId,
//                ListeningPort = localListeningPort
//                // use formatted chainId as certificate name, which can be changed later.  
//            });
//        }

        protected override AsyncServerStreamingCall<ResponseSideChainBlockData> RequestIndexing(RequestCrossChainBlockData requestCrossChainBlockData)
        {
            return Client.RequestIndexingFromSideChain(requestCrossChainBlockData);
        }
    }
    
    public class GrpcClientForParentChain : CrossChainGrpcClient<ResponseParentChainBlockData>
    {
        public GrpcClientForParentChain(string uri, int localChainId)
        {
            LocalChainId = localChainId;
            Client = new CrossChainRpc.CrossChainRpcClient(CreateChannel(uri));
        }

//        public override async Task<IndexingHandShakeReply> TryHandShakeAsync(int remoteChainId, int localListeningPort)
//        {
//            
//        }

//        protected override AsyncDuplexStreamingCall<RequestCrossChainBlockData, ResponseParentChainBlockData> CallWithDuplexStreaming(int milliSeconds = 0)
//        {
//            return milliSeconds == 0
//                ? _client.RequestParentChainDuplexStreaming()
//                : _client.RequestParentChainDuplexStreaming(deadline: DateTime.UtcNow.AddMilliseconds(milliSeconds));
//        }

        protected override AsyncServerStreamingCall<ResponseParentChainBlockData> RequestIndexing(RequestCrossChainBlockData requestCrossChainBlockData)
        {
            return Client.RequestIndexingFromParentChain(requestCrossChainBlockData);
        }
    }

    public interface IGrpcCrossChainClient
    {
        //Task StartDuplexStreamingCall(int chainId, CancellationToken cancellationToken);
        Task<IndexingHandShakeReply> TryHandShakeAsync(int chainId, int localListeningPort);
        Task<bool> StartIndexingRequest(int chainId, ICrossChainDataProducer crossChainDataProducer);
        Task<ChainInitializationResponse> RequestChainInitializationContext(int chainId);
    }
}