using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.ApiVersioning;
using Volo.Abp.AspNetCore;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.DependencyInjection;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.UI;

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