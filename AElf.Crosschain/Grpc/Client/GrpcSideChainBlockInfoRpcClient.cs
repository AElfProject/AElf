using System;
using System.Threading;
using System.Threading.Tasks;
using AElf.Crosschain.Grpc;
using Grpc.Core;

namespace AElf.Crosschain.Grpc.Client
{
    public class GrpcSideChainBlockInfoRpcClient : GrpcCrossChainClient<ResponseSideChainBlockInfo>
    {
        private readonly SideChainBlockInfoRpc.SideChainBlockInfoRpcClient _client;

        public GrpcSideChainBlockInfoRpcClient(Channel channel, ClientBase clientBase) : base(channel, clientBase)
        {
            _client = new SideChainBlockInfoRpc.SideChainBlockInfoRpcClient(channel);
        }

        protected override AsyncDuplexStreamingCall<RequestBlockInfo, ResponseSideChainBlockInfo> Call(int milliSeconds = 0)
        {
            return milliSeconds == 0
                ? _client.IndexDuplexStreaming()
                : _client.IndexDuplexStreaming(deadline: DateTime.UtcNow.AddMilliseconds(milliSeconds));
        }

        protected override AsyncServerStreamingCall<ResponseSideChainBlockInfo> Call(RequestBlockInfo requestBlockInfo)
        {
            return _client.IndexServerStreaming(requestBlockInfo);
        }
    }
}