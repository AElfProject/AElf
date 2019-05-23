using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Cache;
using AElf.CrossChain.Communication.Infrastructure;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace AElf.CrossChain.Communication.Grpc
{
    public abstract class GrpcCrossChainClient<T> : ICrossChainClient where T : IBlockCacheEntity
    {
        protected readonly Channel Channel;
        protected readonly int DialTimeout;
        private readonly int _localChainId;
        private readonly int _localListeningPort;
        private readonly string _host;
        protected IBlockCacheEntityProducer BlockCacheEntityProducer;
        public string TargetUriString => Channel.Target;
        public int ChainId => _localChainId;
        private readonly CrossChainRpc.CrossChainRpcClient _grpcClient;

        protected GrpcCrossChainClient(GrpcCrossChainInitializationContext grpcCrossChainInitializationContext)
        {
            _localChainId = grpcCrossChainInitializationContext.LocalChainId;
            DialTimeout = grpcCrossChainInitializationContext.DialTimeout;
            Channel = CreateChannel(grpcCrossChainInitializationContext.UriStr);
            _grpcClient = new CrossChainRpc.CrossChainRpcClient(Channel);
            _localListeningPort = grpcCrossChainInitializationContext.LocalServerPort;
            _host = grpcCrossChainInitializationContext.LocalServerHost;
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

        public async Task<bool> ConnectAsync()
        {
            try
            {
                var reply = await HandShakeAsync();
                return reply != null && reply.Result;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task CloseAsync()
        {
            await Channel.ShutdownAsync();
        }
        
        public async Task RequestCrossChainDataAsync(long targetHeight)
        {
            var requestData = new CrossChainRequest
            {
                FromChainId = _localChainId,
                NextHeight = targetHeight
            };

            using (var serverStream = RequestIndexing(requestData))
            {
                while (await serverStream.ResponseStream.MoveNext())
                {
                    var response = serverStream.ResponseStream.Current;

//                    var blockCacheEntity = new IBlockCacheEntity
//                    {
//                        ChainId = response.ChainId, 
//                        Height = response.Height,
//                        Payload = response.BlockData.Payload
//                    };
                    // requestCrossChain failed or useless response
                    if (!BlockCacheEntityProducer.TryAddBlockCacheEntity(response))
                    {
                        break;
                    }

                    BlockCacheEntityProducer.Logger.LogTrace(
                        $"Received response from chain {ChainHelpers.ConvertChainIdToBase58(response.ChainId)} at height {response.Height}");
                }
            }
        }
    
        private Task<HandShakeReply> HandShakeAsync()
        {
            var handShakeReply = _grpcClient.CrossChainIndexingShakeAsync(new HandShake
            {
                FromChainId = _localChainId,
                ListeningPort = _localListeningPort,
                Host = _host
            }, new CallOptions().WithDeadline(DateTime.UtcNow.AddSeconds(DialTimeout)));
            return Task.FromResult(handShakeReply);
        }
        
        public Task<ChainInitializationData> RequestChainInitializationContext(int chainId)
        {
            var sideChainInitializationResponse = _grpcClient.RequestChainInitializationContextFromParentChainAsync(
                new SideChainInitializationRequest
                {
                    ChainId = chainId
                }, new CallOptions().WithDeadline(DateTime.UtcNow.AddSeconds(DialTimeout)));
            return Task.FromResult(sideChainInitializationResponse);
        }

        protected abstract AsyncServerStreamingCall<T> RequestIndexing(
            CrossChainRequest crossChainRequest);
    }
    
    public class ClientForSideChain : GrpcCrossChainClient<SideChainBlockData>
    {
        public ClientForSideChain(GrpcCrossChainInitializationContext grpcCrossChainInitializationContext, IBlockCacheEntityProducer blockCacheEntityProducer)
            : base(grpcCrossChainInitializationContext)
        {
            BlockCacheEntityProducer = blockCacheEntityProducer;
        }

        protected override AsyncServerStreamingCall<SideChainBlockData> RequestIndexing(CrossChainRequest crossChainRequest)
        {
            return new CrossChainRpc.CrossChainRpcClient(Channel).RequestIndexingFromSideChainAsync(crossChainRequest,
                new CallOptions().WithDeadline(DateTime.UtcNow.AddSeconds(DialTimeout)));
        }
    }
    
    public class ClientForParentChain : GrpcCrossChainClient<ParentChainBlockData>
    {
        public ClientForParentChain(GrpcCrossChainInitializationContext grpcCrossChainInitializationContext, IBlockCacheEntityProducer blockCacheEntityProducer)
            : base(grpcCrossChainInitializationContext)
        {
            BlockCacheEntityProducer = blockCacheEntityProducer;
        }

        protected override AsyncServerStreamingCall<ParentChainBlockData> RequestIndexing(CrossChainRequest crossChainRequest)
        {
            return new CrossChainRpc.CrossChainRpcClient(Channel).RequestIndexingFromParentChainAsync(crossChainRequest,
                new CallOptions().WithDeadline(DateTime.UtcNow.AddSeconds(DialTimeout)));
        }
    }

    public class GrpcCrossChainInitializationContext
    {
        public string UriStr { get; set; }
        public int LocalChainId { get; set; }
        public int DialTimeout { get; set; }
        public int LocalServerPort { get; set; }
        
        public string LocalServerHost { get; set; }
    }
}