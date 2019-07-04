using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.AspNetCore;
using Volo.Abp.Modularity;

namespace AElf.OS.Rpc
{
    [DependsOn(
        typeof(AbpAspNetCoreModule)
    )]
    public class RpcAElfModule : AElfModule
    {
        private IServiceCollection _serviceCollection = null;

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddCors();
        }

        public override void PostConfigureServices(ServiceConfigurationContext context)
        {
            RpcServerHelper.ConfigureServices(context.Services);
            _serviceCollection = context.Services;
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();
            RpcServerHelper.Configure(app, _serviceCollection);
        }

        public override void OnPostApplicationInitialization(ApplicationInitializationContext context)
        {
        }

        public override void OnApplicationShutdown(ApplicationShutdownContext context)
        {
        }
    }
}