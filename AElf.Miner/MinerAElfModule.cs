using AElf.ChainController;
using AElf.Common;
<<<<<<< HEAD
using AElf.Configuration.Config.Chain;
using AElf.Kernel.BlockService;
using AElf.Kernel.Txn;
=======
using AElf.Common.Application;
using AElf.Kernel;
>>>>>>> dev
using AElf.Miner.Miner;
using AElf.Miner.TxMemPool;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Miner
{
    [DependsOn(typeof(ChainControllerAElfModule))]
    public class MinerAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
<<<<<<< HEAD
            services.AddSingleton<IMinerConfig>(minerConfig);
=======

            services.AddSingleton<ClientManager>();
            services.AddSingleton<ServerManager>();
>>>>>>> dev
            services.AddTransient<TransactionFilter>();
            services.AddSingleton<ISystemTransactionGenerator, FeeClaimingTransactionGenerator>();
            services.AddSingleton<IBlockGenerationService, BlockGenerationService>();
            services.AddTransient<TransactionTypeIdentificationService>();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            // TODO: Shouldn't be here, remove it after module refactor
            var chainId = context.ServiceProvider.GetService<IOptionsSnapshot<ChainOptions>>().Value.ChainId
                .ConvertBase58ToChainId();
            
<<<<<<< HEAD
//            //TODO! should define a interface like RuntimeEnvironment and inject it in GrpcClientManager.
//            context.ServiceProvider.GetService<GrpcClientManager>()
//                .Init(ApplicationHelpers.ConfigPath);
//            context.ServiceProvider.GetService<ServerManager>()
//                .Init(ApplicationHelpers.ConfigPath);
=======
            //TODO! should define a interface like RuntimeEnvironment and inject it in ClientManager.
            context.ServiceProvider.GetService<ClientManager>()
                .Init(ApplicationHelpers.ConfigPath);
            context.ServiceProvider.GetService<ServerManager>()
                .Init(chainId, ApplicationHelpers.ConfigPath);
>>>>>>> dev
        }
    }
}