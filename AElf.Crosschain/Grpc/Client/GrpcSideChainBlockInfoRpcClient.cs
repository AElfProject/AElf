using System;
using System.Threading;
using System.Threading.Tasks;
using AElf.Crosschain.Grpc;
using Grpc.Core;

namespace AElf.Crosschain.Grpc.Client
{
    public class GrpcSideChainBlockInfoRpcClient : GrpcCrossChainClient<ResponseSideChainBlockData>
    {
        private readonly SideChainRpc.SideChainRpcClient _client;

        public GrpcSideChainBlockInfoRpcClient(Channel channel, GrpcClientBase grpcClientBase) : base(channel, grpcClientBase)
        {
            _client = new SideChainRpc.SideChainRpcClient(channel);
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