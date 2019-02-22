using System;
using System.Threading;
using System.Threading.Tasks;
using AElf.Crosschain.Grpc;
using Grpc.Core;

namespace AElf.Crosschain.Grpc.Client
{
    public class GrpcSideChainBlockInfoRpcClient : GrpcCrossChainClient<ResponseSideChainBlockData>
    {
        private readonly CrossChainRpc.CrossChainRpcClient _client;

        public GrpcSideChainBlockInfoRpcClient(Channel channel, CrossChainDataProducer crossChainDataProducer) : base(channel, crossChainDataProducer)
        {
            _client = new CrossChainRpc.CrossChainRpcClient(channel);
        }

        protected override AsyncDuplexStreamingCall<RequestCrossChainBlockData, ResponseSideChainBlockData> Call(int milliSeconds = 0)
        {
            return milliSeconds == 0
                ? _client.RequestSideChainDuplexStreaming()
                : _client.RequestSideChainDuplexStreaming(deadline: DateTime.UtcNow.AddMilliseconds(milliSeconds));
        }

        protected override AsyncServerStreamingCall<ResponseSideChainBlockData> Call(RequestCrossChainBlockData requestCrossChainBlockData)
        {
            return _client.RequestSideChainServerStreaming(requestCrossChainBlockData);
        }
    }
}