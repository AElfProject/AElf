using AElf.Kernel;
using AElf.Modularity;
using AElf.WebApp.Application.Chain.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AElf.WebApp.Application.Chain;

[DependsOn(
    typeof(CoreKernelAElfModule),
    typeof(CoreApplicationWebAppAElfModule),
    typeof(AbpAutoMapperModule)
)]
public class ChainApplicationWebAppAElfModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAutoMapperObjectMapper<ChainApplicationWebAppAElfModule>();

        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<ChainApplicationWebAppAElfModule>(); });

        context.Services
            .AddSingleton<ITransactionResultStatusCacheProvider, TransactionResultStatusCacheProvider>();

        Configure<MultiTransactionOptions>(context.Services.GetConfiguration()
            .GetSection("MultiTransaction"));
    }
}