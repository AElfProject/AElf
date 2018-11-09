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
            : base(logger, targetChainId, interval, cachedBoundedCapacity)
        {
            _client = new SideChainBlockInfoRpc.SideChainBlockInfoRpcClient(channel);
        }

        protected override AsyncDuplexStreamingCall<RequestBlockInfo, ResponseSideChainBlockInfo> Call()
        {
            return _client.IndexDuplexStreaming(deadline: DateTime.Now.AddMilliseconds(1_000));
        }

        protected override AsyncServerStreamingCall<ResponseSideChainBlockInfo> Call(RequestBlockInfo requestBlockInfo)
        {
            return _client.IndexServerStreaming(requestBlockInfo, deadline: DateTime.Now.AddMilliseconds(1_000));
        }
    }
}