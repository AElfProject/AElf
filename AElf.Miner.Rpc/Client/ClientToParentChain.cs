using System;
using AElf.Common.Attributes;
using AElf.Common;
using Grpc.Core;
using ServiceStack;

namespace AElf.Miner.Rpc.Client
{
    [LoggerName("ClientToParentChain")]
    public class ClientToParentChain : ClientBase<ResponseParentChainBlockInfo>
    {
        private readonly ParentChainBlockInfoRpc.ParentChainBlockInfoRpcClient _client;

        public ClientToParentChain(Channel channel, Hash targetChainId, int interval,  int irreversible, int maximalIndexingCount) 
            : base(channel, logger, targetChainId, interval, irreversible, maximalIndexingCount)
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