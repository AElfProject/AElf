using AElf.Common;
using AElf.Common.Application;
using AElf.Configuration.Config.Chain;
using AElf.Kernel.Miner;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.TxPool
{
    public class TxPoolAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var minerConfig = MinerConfig.Default;
            minerConfig.ChainId = ChainConfig.Instance.ChainId.ConvertBase58ToChainId();
            
            var services = context.Services;
            services.AddTransient<TransactionFilter>();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            
            //TODO! should define a interface like RuntimeEnvironment and inject it in ClientManager.
        }


    }
}