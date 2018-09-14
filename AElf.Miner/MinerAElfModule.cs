using AElf.Common.Application;
using AElf.Common.ByteArrayHelpers;
using AElf.Common.Module;
using AElf.Configuration;
using AElf.Configuration.Config.GRPC;
using AElf.Kernel;
using AElf.Miner.Miner;
using AElf.Miner.Rpc.Client;
using AElf.Miner.Rpc.Server;
using Autofac;
using Google.Protobuf;

namespace AElf.Miner
{
    public class MinerAElfModule:IAElfModule
    {
        public void Init(ContainerBuilder builder)
        {
            var minerConfig = MinerConfig.Default;
            if (NodeConfig.Instance.IsMiner)
            {
                minerConfig = new MinerConfig
                {
                    CoinBase = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(NodeConfig.Instance.NodeAccount))
                };
            }
            minerConfig.ChainId = ByteArrayHelpers.FromHexString(NodeConfig.Instance.ChainId);
            builder.RegisterModule(new MinerAutofacModule(minerConfig));
            builder.RegisterType<ClientManager>().SingleInstance().OnActivated(mc =>
                {
                    mc.Instance.Init(dir: ApplicationHelpers.GetDefaultDataDir());
                }
            );
            builder.RegisterType<SideChainBlockInfoRpcServerImpl>().As<SideChainBlockInfoRpcServerImpl>();
            builder.RegisterType<ServerManager>().SingleInstance().OnActivated(mc =>
                {
                    mc.Instance.Init(ApplicationHelpers.GetDefaultDataDir());
                }
            );
        }

        public void Run(ILifetimeScope scope)
        {
        }
    }
}