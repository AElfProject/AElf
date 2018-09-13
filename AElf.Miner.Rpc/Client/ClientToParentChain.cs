using AElf.Common.Attributes;
using Grpc.Core;
using NLog;
namespace AElf.Miner.Rpc.Client
{
    [LoggerName("ClientToParentChain")]
    public class ClientToParentChain : ClientBase<RequestParentChainIndexingInfo, ResponseParentChainIndexingInfo>
    {
        private readonly ParentChainHeaderInfoRpc.ParentChainHeaderInfoRpcClient _client;

        public ClientToParentChain(Channel channel, ILogger logger, string targetChainId, int interval) 
            : base(logger, targetChainId, interval)
        {
            _client = new ParentChainHeaderInfoRpc.ParentChainHeaderInfoRpcClient(channel);
        }

        protected override AsyncDuplexStreamingCall<RequestParentChainIndexingInfo, ResponseParentChainIndexingInfo> Call()
        {
            return _client.Record();
        }
    }
}