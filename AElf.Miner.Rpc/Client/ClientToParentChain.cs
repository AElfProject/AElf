using AElf.Common.Attributes;
using AElf.Common;
using Grpc.Core;
using NLog;
namespace AElf.Miner.Rpc.Client
{
    [LoggerName("ClientToParentChain")]
    public class ClientToParentChain : ClientBase<ResponseParentChainBlockInfo>
    {
        private readonly ParentChainBlockInfoRpc.ParentChainBlockInfoRpcClient _client;

        public ClientToParentChain(Channel channel, ILogger logger, Hash targetChainId, int interval,  int cachedBoundedCapacity) 
            : base(logger, targetChainId, interval, cachedBoundedCapacity)
        {
            _client = new ParentChainBlockInfoRpc.ParentChainBlockInfoRpcClient(channel);
        }

        protected override AsyncDuplexStreamingCall<RequestBlockInfo, ResponseParentChainBlockInfo> Call()
        {
            return _client.RecordDuplexStreaming();
        }

        protected override AsyncServerStreamingCall<ResponseParentChainBlockInfo> Call(RequestBlockInfo requestBlockInfo)
        {
            return _client.RecordServerStreaming(requestBlockInfo);
        }
    }
}