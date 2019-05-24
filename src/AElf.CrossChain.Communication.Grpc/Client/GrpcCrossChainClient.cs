using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Cache;
using AElf.CrossChain.Communication.Infrastructure;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace AElf.CrossChain
{
    public abstract class GrpcCrossChainClient<TData, TClient> : ICrossChainClient where TData : IBlockCacheEntity where TClient : ClientBase<TClient>
    {
        protected Channel Channel;
        protected readonly int DialTimeout;
        private readonly int _localChainId;
        private readonly int _localListeningPort;
        private BasicCrossChainRpc.BasicCrossChainRpcClient _basicGrpcClient;
        protected TClient GrpcClient;
        private readonly string _host;
        protected IBlockCacheEntityProducer BlockCacheEntityProducer;
        public string TargetUriString => Channel.Target;
        public int RemoteChainId { get; }


        protected GrpcCrossChainClient(GrpcCrossChainInitializationContext grpcCrossChainInitializationContext)
        {
            _localChainId = grpcCrossChainInitializationContext.LocalChainId;
            RemoteChainId = grpcCrossChainInitializationContext.RemoteChainId;
            DialTimeout = grpcCrossChainInitializationContext.DialTimeout;
            Channel = CreateChannel(grpcCrossChainInitializationContext.UriStr);
            _basicGrpcClient = new BasicCrossChainRpc.BasicCrossChainRpcClient(Channel);
//            _grpcClient = new CrossChainRpc.CrossChainRpcClient(Channel);
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

        protected abstract void UpdateClient();
        

        public Task<bool> ConnectAsync()
        {
            var reply = _basicGrpcClient.CrossChainHandShakeAsync(new HandShake
            {
                FromChainId = _localChainId,
                ListeningPort = _localListeningPort,
                Host = _host
            }, new CallOptions().WithDeadline(DateTime.UtcNow.AddSeconds(DialTimeout)));
            var res = reply != null && reply.Result;
//            if (!res)
//            {
//                Channel = CreateChannel(Channel.Target);
//                _basicGrpcClient = new BasicCrossChainRpc.BasicCrossChainRpcClient(Channel);
//                UpdateClient();
//            }
            
            return Task.FromResult(res);
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
    
//        private Task<HandShakeReply> HandShakeAsync()
//        {
//            var handShakeReply = GrpcClient.CrossChainIndexingShake(new HandShake
//            {
//                FromChainId = _localChainId,
//                ListeningPort = _localListeningPort,
//                Host = _host
//            }, new CallOptions().WithDeadline(DateTime.UtcNow.AddSeconds(DialTimeout)));
//            return Task.FromResult(handShakeReply);
//        }

        public abstract Task<ChainInitializationData> RequestChainInitializationDataAsync(int chainId);
//        {
//            var sideChainInitializationResponse = GrpcClient.RequestChainInitializationContextFromParentChain(
//                new SideChainInitializationRequest
//                {
//                    ChainId = chainId
//                }, new CallOptions().WithDeadline(DateTime.UtcNow.AddSeconds(DialTimeout)));
//            return Task.FromResult(sideChainInitializationResponse);
//        }

        protected abstract AsyncServerStreamingCall<TData> RequestIndexing(
            CrossChainRequest crossChainRequest);

//        private HandShakeReply DoHandShake(HandShake handShake)
//        {
//            return CrossChainHandShakeAsync(handShake,
//                new CallOptions().WithDeadline(DateTime.UtcNow.AddSeconds(DialTimeout)));
//        }
    }
    
    public class ClientForSideChain : GrpcCrossChainClient<SideChainBlockData, SideChainRpc.SideChainRpcClient>
    {
        public ClientForSideChain(GrpcCrossChainInitializationContext grpcCrossChainInitializationContext, IBlockCacheEntityProducer blockCacheEntityProducer)
            : base(grpcCrossChainInitializationContext)
        {
            GrpcClient = new SideChainRpc.SideChainRpcClient(Channel);
            BlockCacheEntityProducer = blockCacheEntityProducer;
        }

        protected override void UpdateClient()
        {
            GrpcClient = new SideChainRpc.SideChainRpcClient(Channel);
        }

        public override Task<ChainInitializationData> RequestChainInitializationDataAsync(int chainId)
        {
            throw new NotImplementedException();
        }

        protected override AsyncServerStreamingCall<SideChainBlockData> RequestIndexing(CrossChainRequest crossChainRequest)
        {
            return GrpcClient.RequestIndexingFromSideChainAsync(crossChainRequest,
                new CallOptions().WithDeadline(DateTime.UtcNow.AddSeconds(DialTimeout)));
        }

//        protected override HandShakeReply DoHandShake(HandShake handShake)
//        {
//            return GrpcClient.CrossChainHandShake(handShake,
//                new CallOptions().WithDeadline(DateTime.UtcNow.AddSeconds(DialTimeout)));
//        }
    }
    
    public class ClientForParentChain : GrpcCrossChainClient<ParentChainBlockData, ParentChainRpc.ParentChainRpcClient>
    {
        public ClientForParentChain(GrpcCrossChainInitializationContext grpcCrossChainInitializationContext, IBlockCacheEntityProducer blockCacheEntityProducer)
            : base(grpcCrossChainInitializationContext)
        {
            GrpcClient = new ParentChainRpc.ParentChainRpcClient(Channel);
            BlockCacheEntityProducer = blockCacheEntityProducer;
        }

        protected override AsyncServerStreamingCall<ParentChainBlockData> RequestIndexing(CrossChainRequest crossChainRequest)
        {
            return GrpcClient.RequestIndexingFromParentChainAsync(crossChainRequest,
                new CallOptions().WithDeadline(DateTime.UtcNow.AddSeconds(DialTimeout)));
        }

        protected override void UpdateClient()
        {
            GrpcClient = new ParentChainRpc.ParentChainRpcClient(Channel);
        }

        public override Task<ChainInitializationData> RequestChainInitializationDataAsync(int chainId)
        {
            var sideChainInitializationResponse = GrpcClient.RequestChainInitializationDataFromParentChainAsync(
                new SideChainInitializationRequest
                {
                    ChainId = chainId
                }, new CallOptions().WithDeadline(DateTime.UtcNow.AddSeconds(DialTimeout)));
            return Task.FromResult(sideChainInitializationResponse);
        }
        
//        protected override HandShakeReply DoHandShake(HandShake handShake)
//        {
//            return GrpcClient.CrossChainHandShake(handShake,
//                new CallOptions().WithDeadline(DateTime.UtcNow.AddSeconds(DialTimeout)));
//        }
    }
    
    public class GrpcCrossChainInitializationContext
    {
        public string UriStr { get; set; }
        public int LocalChainId { get; set; }
        
        public int RemoteChainId { get; set; }
        public int DialTimeout { get; set; }
        public int LocalServerPort { get; set; }
        
        public string LocalServerHost { get; set; }
    }
}