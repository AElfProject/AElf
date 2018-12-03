using AElf.Common;
using AElf.Common.Application;
using AElf.Common.Module;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
using AElf.Miner.Miner;
using AElf.Miner.Rpc;
using AElf.Miner.Rpc.Client;
using AElf.Miner.Rpc.Server;
using AElf.Miner.TxMemPool;
using Autofac;
using Google.Protobuf;

namespace AElf.Miner
{
    public class MinerAElfModule : IAElfModule
    {
        public void Init(ContainerBuilder builder)
        {
            var minerConfig = MinerConfig.Default;
            if (NodeConfig.Instance.IsMiner)
            {
                minerConfig = new MinerConfig
                {
                    CoinBase =Address.Parse(NodeConfig.Instance.NodeAccount) 
                };
            }

            minerConfig.ChainId = new Hash()
            {
                Value = ByteString.CopyFrom(ChainConfig.Instance.ChainId.DecodeBase58())
            };
            builder.RegisterModule(new MinerRpcAutofacModule());

            builder.RegisterType<ClientManager>().SingleInstance().OnActivated(mc =>
                {
                    mc.Instance.Init(dir: ApplicationHelpers.GetDefaultConfigPath());
                }
            );
            builder.RegisterType<ServerManager>().SingleInstance().OnActivated(mc =>
                {
                    mc.Instance.Init(ApplicationHelpers.GetDefaultConfigPath());
                }
            );
            builder.RegisterModule(new MinerAutofacModule(minerConfig));

            var txPoolConfig = TxPoolConfig.Default;
            txPoolConfig.FeeThreshold = 0;
            txPoolConfig.PoolLimitSize = TransactionPoolConfig.Instance.PoolLimitSize;
            txPoolConfig.Maximal = TransactionPoolConfig.Instance.Maximal;
            txPoolConfig.EcKeyPair = TransactionPoolConfig.Instance.EcKeyPair;
            builder.RegisterInstance(txPoolConfig).As<ITxPoolConfig>();
        }

        public void Run(ILifetimeScope scope)
        {
        }
    }
}