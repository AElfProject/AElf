using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Modularity;

namespace AElf.WebApp.Web
{
    public class WebAppAbpAspNetCoreMvcModule : AbpAspNetCoreMvcModule
    {
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<AbpAspNetCoreMvcModule>();
            base.ConfigureServices(context);
        }
    }
}