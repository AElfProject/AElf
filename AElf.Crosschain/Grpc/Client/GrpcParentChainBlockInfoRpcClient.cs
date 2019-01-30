using System;
using System.Threading;
using Grpc.Core;

namespace AElf.Crosschain.Grpc.Client
{
    public class GrpcParentChainBlockInfoRpcClient : GrpcCrossChainClient<ResponseParentChainBlockInfo>
    {
        private readonly ParentChainBlockInfoRpc.ParentChainBlockInfoRpcClient _client;

        public GrpcParentChainBlockInfoRpcClient(Channel channel, ClientBase clientBase) : base(channel, clientBase)
        {
            _client = new ParentChainBlockInfoRpc.ParentChainBlockInfoRpcClient(channel);
        }

        protected override AsyncDuplexStreamingCall<RequestBlockInfo, ResponseParentChainBlockInfo> Call(int milliSeconds = 0)
        {
            return milliSeconds == 0
                ? _client.RecordDuplexStreaming()
                : _client.RecordDuplexStreaming(deadline: DateTime.UtcNow.AddMilliseconds(milliSeconds));
        }

        protected override AsyncServerStreamingCall<ResponseParentChainBlockInfo> Call(RequestBlockInfo requestBlockInfo)
        {
            return _client.RecordServerStreaming(requestBlockInfo);
        }
    }
}