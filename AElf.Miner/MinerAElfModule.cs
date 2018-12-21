using AElf.Common;
using AElf.Common.Application;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
using AElf.Miner.Miner;
using AElf.Miner.Rpc;
using AElf.Miner.Rpc.Client;
using AElf.Miner.Rpc.Server;
using AElf.Miner.TxMemPool;
using AElf.Modularity;
using Autofac;
using Google.Protobuf;
using Volo.Abp.Modularity;

namespace AElf.Miner
{
    public class MinerAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            
        }

        public void Init(ContainerBuilder builder)
        {
            //TODO! move into configure services. spilt static configuration and runtime variables
            
            var minerConfig = MinerConfig.Default;
            if (NodeConfig.Instance.IsMiner)
            {
                minerConfig = new MinerConfig
                {
                    CoinBase = Address.Parse(NodeConfig.Instance.NodeAccount) 
                };
            }

            minerConfig.ChainId = new Hash()
            {
                Value = ByteString.CopyFrom(ChainConfig.Instance.ChainId.DecodeBase58())
            };
            builder.RegisterModule(new MinerRpcAutofacModule());

            builder.RegisterType<ClientManager>().SingleInstance().OnActivated(mc =>
                {
                    mc.Instance.Init(dir: ApplicationHelpers.ConfigPath);
                }
            );
            builder.RegisterType<ServerManager>().SingleInstance().OnActivated(mc =>
                {
                    mc.Instance.Init(ApplicationHelpers.ConfigPath);
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

    }
}