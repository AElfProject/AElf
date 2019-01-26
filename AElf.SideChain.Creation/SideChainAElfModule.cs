using AElf.Kernel;
using AElf.Modularity;
using Easy.MessageHub;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.SideChain.Creation
{
    [DependsOn(typeof(KernelAElfModule))]
    public class SideChainAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            base.ConfigureServices(context);
            context.Services.AddTransient<ChainCreationEventListener>();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var evListener = context.ServiceProvider.GetRequiredService<ChainCreationEventListener>();
            MessageHub.Instance.Subscribe<IBlock>(async (t) =>
            {
                await evListener.OnBlockAppended(t);
            });
        }

    }
}