using AElf.Kernel.Token;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.Kernel
{
    [DependsOn(
        typeof(KernelCoreTestAElfModule))]
    public class KernelCoreWithChainTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<INativeTokenSymbolProvider, DefaultNativeTokenSymbolProvider>();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var kernelTestHelper = context.ServiceProvider.GetService<KernelTestHelper>();
            AsyncHelper.RunSync(() => kernelTestHelper.MockChainAsync());
        }
    }
}