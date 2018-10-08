using System;
using AElf.Common;
using AElf.Common.Application;
using AElf.Common.Module;
using AElf.Configuration;
using AElf.Miner.Miner;
using AElf.Miner.Rpc;
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
                    CoinBase =Address.Loads(NodeConfig.Instance.NodeAccount) 
                };
            }
            minerConfig.ChainId = new Hash()
            {
                Value = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(NodeConfig.Instance.ChainId))
            };
            builder.RegisterModule(new MinerRpcAutofacModule());

            builder.RegisterType<ClientManager>().SingleInstance().OnActivated(mc =>
                {
                    mc.Instance.Init(dir: ApplicationHelpers.GetDefaultDataDir());
                }
            );
            builder.RegisterType<ServerManager>().SingleInstance().OnActivated(mc =>
                {
                    mc.Instance.Init(ApplicationHelpers.GetDefaultDataDir());
                }
            );
            builder.RegisterModule(new MinerAutofacModule(minerConfig));
        }

        public void Run(ILifetimeScope scope)
        {
        }
    }
}