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
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Miner
{
    public class MinerAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            
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

            var services = context.Services;
            services.AddSingleton<ClientManager>();
            services.AddSingleton<ServerManager>();
            services.AddSingleton<ClientManager>();

            

            var txPoolConfig = TxPoolConfig.Default;
            txPoolConfig.FeeThreshold = 0;
            txPoolConfig.PoolLimitSize = TransactionPoolConfig.Instance.PoolLimitSize;
            txPoolConfig.Maximal = TransactionPoolConfig.Instance.Maximal;
            txPoolConfig.EcKeyPair = TransactionPoolConfig.Instance.EcKeyPair;

            services.AddSingleton<ITxPoolConfig>(txPoolConfig);
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            
            //TODO! should define a interface like RuntimeEnvironment and inject it in ClientManager.
            context.ServiceProvider.GetService<ClientManager>()
                .Init(ApplicationHelpers.ConfigPath);
            context.ServiceProvider.GetService<ServerManager>()
                .Init(ApplicationHelpers.ConfigPath);
        }


    }
}