using AElf.Kernel;
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
            var services = context.Services;
            services.AddSingleton<ITxHub, TxHub>();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            
            //TODO! should define a interface like RuntimeEnvironment and inject it in ClientManager.
        }


    }
}