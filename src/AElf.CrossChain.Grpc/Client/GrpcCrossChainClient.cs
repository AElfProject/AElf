using System;
using System.Threading;
using System.Threading.Tasks;
using AElf.Standards.ACS7;
using AElf.CrossChain.Communication.Infrastructure;
using AElf.Kernel;
using Grpc.Core;

namespace AElf.CrossChain.Grpc.Client
{
    public class GrpcClientInitializationContext
    {
        public string UriStr { get; set; }
        public int LocalChainId { get; set; }

        public int RemoteChainId { get; set; }
        public int DialTimeout { get; set; }
        public int ListeningPort { get; set; }
    }

    public abstract class GrpcCrossChainClient<TData, TClient> : ICrossChainClient where TData : ICrossChainBlockEntity
        where TClient : ClientBase<TClient>
    {
        protected Channel Channel;
        protected readonly int DialTimeout;
        protected TClient GrpcClient;

        private readonly int _localChainId;
        private readonly int _listeningPort;
        private readonly BasicCrossChainRpc.BasicCrossChainRpcClient _basicGrpcClient;

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
            _listeningPort = grpcClientInitializationContext.ListeningPort;
        }

        /// <summary>
        /// Create a new channel.
        /// </summary>
        /// <param name="uriStr"></param>
        /// <returns></returns>
        private static Channel CreateChannel(string uriStr)
        {
            return new Channel(uriStr, ChannelCredentials.Insecure);
        }

        /// <summary>
        /// Connect with target chain.
        /// </summary>
        /// <returns></returns>
        public Task ConnectAsync()
        {
            return RequestAsync(HandshakeAsync);
        }

        /// <summary>
        /// Close client and clear channel.
        /// </summary>
        /// <returns></returns>
        public async Task CloseAsync()
        {
            IsConnected = false;
            await Channel.ShutdownAsync();
        }

        /// <summary>
        /// Request target chain for cross chain data from target height. 
        /// </summary>
        /// <param name="targetHeight"></param>
        /// <param name="crossChainBlockDataEntityHandler"></param>
        /// <returns></returns>
        public Task RequestCrossChainDataAsync(long targetHeight,
            Func<ICrossChainBlockEntity, bool> crossChainBlockDataEntityHandler)
        {
            var requestData = new CrossChainRequest
            {
                ChainId = _localChainId,
                NextHeight = targetHeight
            };

            return RequestAsync(() => RequestCrossChainDataAsync(requestData, crossChainBlockDataEntityHandler));
        }

        /// <summary>
        /// Asynchronous request method.
        /// </summary>
        /// <param name="requestFunc"></param>
        /// <returns></returns>
        private async Task RequestAsync(Func<Task> requestFunc)
        {
            try
            {
                await requestFunc();
            }
            catch (RpcException e)
            {
                IsConnected = false;
                throw new GrpcCrossChainRequestException(e.Message, e);
            }
        }

        /// <summary>
        /// Create options for grpc request.
        /// </summary>
        /// <returns></returns>
        protected CallOptions CreateOption()
        {
            return new CallOptions().WithDeadline(TimestampHelper.GetUtcNow().ToDateTime()
                .AddMilliseconds(DialTimeout));
        }

        private async Task RequestCrossChainDataAsync(CrossChainRequest crossChainRequest,
            Func<ICrossChainBlockEntity, bool> crossChainBlockDataEntityHandler)
        {
            using (var serverStream = RequestIndexing(crossChainRequest))
            {
                while (await serverStream.ResponseStream.MoveNext(CancellationToken.None))
                {
                    var response = serverStream.ResponseStream.Current;

                    // requestCrossChain failed or useless response
                    if (!crossChainBlockDataEntityHandler(response))
                    {
                        break;
                    }
                }
            }
        }

        private async Task HandshakeAsync()
        {
            var reply = await _basicGrpcClient.CrossChainHandShakeAsync(new HandShake
            {
                ChainId = _localChainId,
                ListeningPort = _listeningPort
            }, CreateOption());
            IsConnected = reply != null && reply.Status == HandShakeReply.Types.HandShakeStatus.Success;
        }

        public abstract Task<ChainInitializationData> RequestChainInitializationDataAsync(int chainId);

        protected abstract AsyncServerStreamingCall<TData> RequestIndexing(CrossChainRequest crossChainRequest);
    }

    public class ClientForSideChain : GrpcCrossChainClient<SideChainBlockData, SideChainRpc.SideChainRpcClient>
    {
        public ClientForSideChain(GrpcClientInitializationContext grpcClientInitializationContext)
            : base(grpcClientInitializationContext)
        {
            GrpcClient = new SideChainRpc.SideChainRpcClient(Channel);
        }

        public override Task<ChainInitializationData> RequestChainInitializationDataAsync(int chainId)
        {
            throw new NotImplementedException();
        }

        protected override AsyncServerStreamingCall<SideChainBlockData> RequestIndexing(
            CrossChainRequest crossChainRequest)
        {
            return GrpcClient.RequestIndexingFromSideChain(crossChainRequest, CreateOption());
        }
    }

    public class ClientForParentChain : GrpcCrossChainClient<ParentChainBlockData, ParentChainRpc.ParentChainRpcClient>
    {
        public ClientForParentChain(GrpcClientInitializationContext grpcClientInitializationContext)
            : base(grpcClientInitializationContext)
        {
            GrpcClient = new ParentChainRpc.ParentChainRpcClient(Channel);
        }

        protected override AsyncServerStreamingCall<ParentChainBlockData> RequestIndexing(
            CrossChainRequest crossChainRequest)
        {
            return GrpcClient.RequestIndexingFromParentChain(crossChainRequest, CreateOption());
        }

        public override async Task<ChainInitializationData> RequestChainInitializationDataAsync(int chainId)
        {
            try
            {
                var sideChainInitializationResponse =
                    await GrpcClient.RequestChainInitializationDataFromParentChainAsync(
                        new SideChainInitializationRequest
                        {
                            ChainId = chainId
                        });
                return sideChainInitializationResponse;
            }
            catch (RpcException e)
            {
                throw new GrpcCrossChainRequestException("Request initialization data failed.", e);
            }
        }
    }
}