using AElf.Common.Attributes;
using Grpc.Core;
using NLog;

namespace AElf.Miner.Rpc.Client
{
    [LoggerName("ClientToSideChain")]
    public class ClientToSideChain : ClientBase<RequestSideChainIndexingInfo, ResponseSideChainIndexingInfo>
    {
        private readonly SideChainHeaderInfoRpc.SideChainHeaderInfoRpcClient _client;

        public ClientToSideChain(Channel channel, ILogger logger, string targetChainId, int interval) 
            : base(logger, targetChainId, interval)
        {
            _client = new SideChainHeaderInfoRpc.SideChainHeaderInfoRpcClient(channel);
        }

        protected override AsyncDuplexStreamingCall<RequestSideChainIndexingInfo, ResponseSideChainIndexingInfo> Call()
        {
            return _client.Index();
        }
    }
}