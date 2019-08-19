using AElf.Kernel.Node.Infrastructure;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.CrossChain.Communication
{
    [DependsOn(typeof(CrossChainAElfModule))]
    [Dependency(ServiceLifetime.Singleton, TryRegister = true)]
    public class CrossChainCommunicationModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IChainInitializationDataPlugin, CrossChainPlugin>();
            context.Services.AddTransient<INodePlugin, CrossChainPlugin>();
        }
    }
}