using AElf.Kernel;
using AElf.Modularity;
using AElf.WebApp.Application.Chain;
using AElf.WebApp.Application.FullNode.Services;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Modularity;

namespace AElf.WebApp.Application.FullNode;

[DependsOn(
    typeof(CoreKernelAElfModule),
    typeof(CoreApplicationWebAppAElfModule),
    typeof(ChainApplicationWebAppAElfModule)
)]
public class FullNodeApplicationWebAppAElfModule : AElfModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.PreConfigure<IMvcBuilder>(options =>
        {
            options.PartManager.ApplicationParts.Add(new AssemblyPart(typeof(FullNodeApplicationWebAppAElfModule).Assembly));
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAssemblyOf<ContractMethodAppService>();
        context.Services.AddControllers();
        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(FullNodeApplicationWebAppAElfModule).Assembly,
                setting => { setting.UrlControllerNameNormalizer = _ => "fullNode"; });
        });
    }
}