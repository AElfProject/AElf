using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Cache.Application;
using AElf.CrossChain.Cache;
using AElf.Kernel;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcClientInitializationContext
    {
        public string UriStr { get; set; }
        public int LocalChainId { get; set; }
        
        public int RemoteChainId { get; set; }
        public int DialTimeout { get; set; }
        public int LocalServerPort { get; set; }
        
        public string LocalServerHost { get; set; }
    }
    
    public abstract class GrpcCrossChainClient<TData, TClient> : ICrossChainClient where TData : IBlockCacheEntity where TClient : ClientBase<TClient>
    {
        protected Channel Channel;
        protected readonly int DialTimeout;
        protected TClient GrpcClient;
        protected IBlockCacheEntityProducer BlockCacheEntityProducer;

        private readonly int _localChainId;
        private readonly int _localListeningPort;
        private readonly BasicCrossChainRpc.BasicCrossChainRpcClient _basicGrpcClient;
        private readonly string _host;
        
        public string TargetUriString => Channel.Target;
        public bool IsConnected { get; private set; }
        public int RemoteChainId { get; }


        protected GrpcCrossChainClient(GrpcClientInitializationContext grpcClientInitializationContext)
        {
            _localChainId = grpcClientInitializationContext.LocalChainId;
            RemoteChainId = grpcClientInitializationContext.RemoteChainId;
            DialTimeout = grpcClientInitializationContext.DialTimeout;
            Channel = CreateChannel(grpcClientInitializationContext.UriStr);
            _basicGrpcClient = new BasicCrossChainRpc.BasicCrossChainRpcClient(Channel);
            _localListeningPort = grpcClientInitializationContext.LocalServerPort;
            _host = grpcClientInitializationContext.LocalServerHost;
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

        private static Channel CreateChannel(string uriStr)
        {
            return new Channel(uriStr, ChannelCredentials.Insecure);
        }

        public Task ConnectAsync()
        {
            return RequestAsync(HandshakeAsync);
        }
        
        public async Task CloseAsync()
        {
            await Channel.ShutdownAsync();
        }
        
        public Task RequestCrossChainDataAsync(long targetHeight)
        {
            var requestData = new CrossChainRequest
            {
                FromChainId = _localChainId,
                NextHeight = targetHeight
            };

            return RequestAsync(() => RequestCrossChainDataAsync(requestData));
        }

        private async Task RequestCrossChainDataAsync(CrossChainRequest crossChainRequest)
        {
            using (var serverStream = RequestIndexing(crossChainRequest))
            {
                while (await serverStream.ResponseStream.MoveNext())
                {
                    var response = serverStream.ResponseStream.Current;

                    // requestCrossChain failed or useless response
                    if (!BlockCacheEntityProducer.TryAddBlockCacheEntity(response))
                    {
                        break;
                    }
                }
            }
        }
        
        private Task HandshakeAsync()
        {
            var reply = _basicGrpcClient.CrossChainHandShakeAsync(new HandShake
            {
                FromChainId = _localChainId,
                ListeningPort = _localListeningPort,
                Host = _host
            }, CreateOption());
            var res = reply != null && reply.Result;
            IsConnected = res;
            return Task.FromResult(res);
        }

        
        private async Task RequestAsync(Func<Task> requestFunc)
        {
            try
            {
                await requestFunc();
            }
            catch (RpcException)
            {
                IsConnected = false;
                throw;
            }
        }

        protected CallOptions CreateOption()
        {
            return new CallOptions().WithDeadline(TimestampHelper.GetUtcNow().ToDateTime().AddMilliseconds(DialTimeout));
        }

        public abstract Task<ChainInitializationData> RequestChainInitializationDataAsync(int chainId);

        protected abstract AsyncServerStreamingCall<TData> RequestIndexing(CrossChainRequest crossChainRequest);
    }
    
    public class ClientForSideChain : GrpcCrossChainClient<SideChainBlockData, SideChainRpc.SideChainRpcClient>
    {
        public ClientForSideChain(GrpcClientInitializationContext grpcClientInitializationContext, IBlockCacheEntityProducer blockCacheEntityProducer)
            : base(grpcClientInitializationContext)
        {
            GrpcClient = new SideChainRpc.SideChainRpcClient(Channel);
            BlockCacheEntityProducer = blockCacheEntityProducer;
        }

        public override Task<ChainInitializationData> RequestChainInitializationDataAsync(int chainId)
        {
            throw new NotImplementedException();
        }

        protected override AsyncServerStreamingCall<SideChainBlockData> RequestIndexing(CrossChainRequest crossChainRequest)
        {
            return GrpcClient.RequestIndexingFromSideChainAsync(crossChainRequest, CreateOption());
        }
    }
    
    public class ClientForParentChain : GrpcCrossChainClient<ParentChainBlockData, ParentChainRpc.ParentChainRpcClient>
    {
        public ClientForParentChain(GrpcClientInitializationContext grpcClientInitializationContext, IBlockCacheEntityProducer blockCacheEntityProducer)
            : base(grpcClientInitializationContext)
        {
            GrpcClient = new ParentChainRpc.ParentChainRpcClient(Channel);
            BlockCacheEntityProducer = blockCacheEntityProducer;
        }

        protected override AsyncServerStreamingCall<ParentChainBlockData> RequestIndexing(CrossChainRequest crossChainRequest)
        {
            return GrpcClient.RequestIndexingFromParentChainAsync(crossChainRequest, CreateOption());
        }

        public override Task<ChainInitializationData> RequestChainInitializationDataAsync(int chainId)
        {
            var sideChainInitializationResponse = GrpcClient.RequestChainInitializationDataFromParentChainAsync(
                new SideChainInitializationRequest
                {
                    ChainId = chainId
                }, CreateOption());
            return Task.FromResult(sideChainInitializationResponse);
        }
    }
}