using AElf.Common.Application;
using AElf.Common.ByteArrayHelpers;
using AElf.Common.Module;
using AElf.Configuration;
using AElf.Miner.Miner;
using AElf.Miner.Rpc;
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
            builder.RegisterModule(new MinerRpcAutofacModule());
        }

        public void Run(ILifetimeScope scope)
        {
        }
    }
}