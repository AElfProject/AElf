using AElf.Common.Attributes;
using AElf.Configuration.Config.GRPC;
using AElf.Kernel;
using Grpc.Core;
using NLog;

namespace AElf.Miner.Rpc.Server
{
    [LoggerName("ParentChainServer")]
    public class ParentChainServer : ServerBase
    {
        private readonly ParentChainHeaderInfoRpcServerImpl _parentChainHeaderInfoRpcServerImpl;
        public ParentChainServer(ILogger logger, ParentChainHeaderInfoRpcServerImpl parentChainHeaderInfoRpcServerImpl)
            : base(logger)
        {
            _parentChainHeaderInfoRpcServerImpl = parentChainHeaderInfoRpcServerImpl;
        }
        
        protected override ServerServiceDefinition BindService()
        {
            return ParentChainHeaderInfoRpc.BindService(_parentChainHeaderInfoRpcServerImpl);
        }

        protected override void InitServerImpl(Hash chainId)
        {
            _parentChainHeaderInfoRpcServerImpl.Init(chainId);
        }

        public override void StartUp()
        {
            base.StartUp(GrpcLocalConfig.Instance.LocalServerIP, GrpcLocalConfig.Instance.LocalParentChainServerPort);
        }
    }
}