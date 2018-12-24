using System;

using AElf.Common;
using Grpc.Core;
namespace AElf.Miner.Rpc.Client
{
    
    public class ClientToSideChain : ClientBase<ResponseSideChainBlockInfo>
    {
        private readonly SideChainBlockInfoRpc.SideChainBlockInfoRpcClient _client;

        public ClientToSideChain(Channel channel, Hash targetChainId, int interval, int irreversible, int maximalIndexingCount) 
            : base(channel, targetChainId, interval, irreversible, maximalIndexingCount)
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