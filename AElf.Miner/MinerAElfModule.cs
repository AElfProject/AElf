using AElf.ChainController;
using AElf.Common;
using AElf.Configuration.Config.Chain;
using AElf.Kernel.BlockService;
using AElf.Kernel.Txn;
using AElf.Miner.Miner;
using AElf.Miner.TxMemPool;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Miner
{
    [DependsOn(typeof(ChainControllerAElfModule))]
    public class MinerAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var minerConfig = MinerConfig.Default;
            minerConfig.ChainId = ChainConfig.Instance.ChainId.ConvertBase58ToChainId();
            
            var services = context.Services;
            services.AddSingleton<IMinerConfig>(minerConfig);
            services.AddTransient<TransactionFilter>();
            services.AddSingleton<ISystemTransactionGenerator, FeeClaimingTransactionGenerator>();
            services.AddSingleton<IBlockGenerationService, BlockGenerationService>();
            services.AddTransient<TransactionTypeIdentificationService>();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            
//            //TODO! should define a interface like RuntimeEnvironment and inject it in GrpcClientManager.
//            context.ServiceProvider.GetService<GrpcClientManager>()
//                .Init(ApplicationHelpers.ConfigPath);
//            context.ServiceProvider.GetService<ServerManager>()
//                .Init(ApplicationHelpers.ConfigPath);
        }
    }
}