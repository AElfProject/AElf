using System;
using Grpc.Core;

namespace AElf.CrossChain.Grpc.Client
{
    public class GrpcParentChainBlockInfoRpcClient : GrpcCrossChainClient<ResponseParentChainBlockData>
    {
        private readonly CrossChainRpc.CrossChainRpcClient _client;

        public GrpcParentChainBlockInfoRpcClient(Channel channel, CrossChainDataProducer crossChainDataProducer) : base(channel, crossChainDataProducer)
        {
            _client = new CrossChainRpc.CrossChainRpcClient(channel);
        }

        protected override AsyncDuplexStreamingCall<RequestCrossChainBlockData, ResponseParentChainBlockData> Call(int milliSeconds = 0)
        {
            return milliSeconds == 0
                ? _client.RequestParentChainDuplexStreaming()
                : _client.RequestParentChainDuplexStreaming(deadline: DateTime.UtcNow.AddMilliseconds(milliSeconds));
        }

        protected override AsyncServerStreamingCall<ResponseParentChainBlockData> Call(RequestCrossChainBlockData requestCrossChainBlockData)
        {
            return _client.RequestParentChainServerStreaming(requestCrossChainBlockData);
        }
    }
}