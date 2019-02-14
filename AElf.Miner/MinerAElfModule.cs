using AElf.ChainController;
using AElf.Common;
using AElf.Common.Application;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
using AElf.Kernel;
using AElf.Miner.Miner;
using AElf.Miner.Rpc.Client;
using AElf.Miner.Rpc.Server;
using AElf.Miner.TxMemPool;
using AElf.Modularity;
using Google.Protobuf;
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

            services.AddSingleton<ClientManager>();
            services.AddSingleton<ServerManager>();
            services.AddTransient<TransactionFilter>();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            // TODO: Shouldn't be here, remove it after module refactor
            var chainId = context.ServiceProvider.GetService<IOptionsSnapshot<ChainOptions>>().Value.ChainId
                .ConvertBase58ToChainId();
            
            //TODO! should define a interface like RuntimeEnvironment and inject it in ClientManager.
            context.ServiceProvider.GetService<ClientManager>()
                .Init(ApplicationHelpers.ConfigPath);
            context.ServiceProvider.GetService<ServerManager>()
                .Init(chainId, ApplicationHelpers.ConfigPath);
        }


    }
}