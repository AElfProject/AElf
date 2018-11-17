using System;
using AElf.Common.Attributes;
using AElf.Common;
using Grpc.Core;
using NLog;

namespace AElf.Miner.Rpc.Client
{
    [LoggerName("ClientToSideChain")]
    public class ClientToSideChain : ClientBase<ResponseSideChainBlockInfo>
    {
        private readonly SideChainBlockInfoRpc.SideChainBlockInfoRpcClient _client;

        public ClientToSideChain(Channel channel, ILogger logger, Hash targetChainId, int interval, int cachedBoundedCapacity) 
            : base(channel, logger, targetChainId, interval, cachedBoundedCapacity)
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