using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AElf.WebApp.Application.Net
{
    [DependsOn(typeof(CoreApplicationWebAppAElfModule), typeof(AbpAutoMapperModule))]
    public class NetApplicationWebAppAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAutoMapperObjectMapper<NetApplicationWebAppAElfModule>();

            Configure<AbpAutoMapperOptions>(options => { options.AddMaps<NetApplicationWebAppAElfModule>(true); });
        }
    }
}