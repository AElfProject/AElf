using AElf.Kernel;
using AElf.Modularity;
using AElf.WebApp.Application.Chain.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.WebApp.Application.Chain
{
    [DependsOn(typeof(CoreKernelAElfModule), typeof(CoreApplicationWebAppAElfModule))]
    public class ChainApplicationWebAppAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services
                .AddSingleton<ITransactionResultStatusCacheProvider, TransactionResultStatusCacheProvider>();
        }
    }
}