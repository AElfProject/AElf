using System.Collections.Generic;
using System.IO;
using AElf.Common.Application;
using AElf.Common.Attributes;
using AElf.Configuration.Config.GRPC;
using AElf.Cryptography.Certificate;
using AElf.Kernel;
using AElf.Miner.Rpc.Exceptions;
using Grpc.Core;
using NLog;

namespace AElf.Miner.Rpc.Server
{
    [LoggerName("SideChainServer")]
    public class SideChainServer : ServerBase
    {
        private readonly SideChainHeaderInfoRpcServerImpl _sideChainHeaderInfoRpcServerImpl;
        public SideChainServer(ILogger logger, SideChainHeaderInfoRpcServerImpl sideChainHeaderInfoRpcServerImpl) 
            : base(logger)
        {
            _sideChainHeaderInfoRpcServerImpl = sideChainHeaderInfoRpcServerImpl;
        }
        
        protected override ServerServiceDefinition BindService()
        {
            return SideChainHeaderInfoRpc.BindService(_sideChainHeaderInfoRpcServerImpl);
        }

        protected override void InitServerImpl(Hash chainId)
        {
            _sideChainHeaderInfoRpcServerImpl.Init(chainId);
        }

        public override void StartUp()
        {
            if(GrpcLocalConfig.Instance.SideChainServer)
                base.StartUp(GrpcLocalConfig.Instance.LocalServerIP, GrpcLocalConfig.Instance.LocalSideChainServerPort);
        }
    }
}