using AElf.Kernel;
using AElf.Modularity;
using AElf.WebApp.Application.Chain.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using Volo.Abp.AutoMapper;

namespace AElf.WebApp.Application.Chain
{
    [DependsOn(typeof(CoreKernelAElfModule), typeof(CoreApplicationWebAppAElfModule), typeof(AbpAutoMapperModule))]
    public class ChainApplicationWebAppAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAutoMapperObjectMapper<ChainApplicationWebAppAElfModule>();

            Configure<AbpAutoMapperOptions>(options =>
            {
                options.AddMaps<ChainApplicationWebAppAElfModule>(true);
            });
            
            context.Services
                .AddSingleton<ITransactionResultStatusCacheProvider, TransactionResultStatusCacheProvider>();
        }
    }
}