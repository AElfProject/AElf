using AElf.Kernel;
using AElf.Modularity;
using AElf.Network;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.AspNetCore;
using Volo.Abp.AspNetCore.Modularity;
using Volo.Abp.Modularity;

namespace AElf.RPC
{
    [DependsOn(
        typeof(AbpAspNetCoreModule),
        typeof(CoreKernelAElfModule),
        //TODO: remove it
        typeof(NetworkAElfModule))]
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
            RpcServerHelpers.ConfigureServices(context.Services);

            _serviceCollection = context.Services;
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();

            RpcServerHelpers.Configure(app, _serviceCollection);
        }

        public override void OnPostApplicationInitialization(ApplicationInitializationContext context)
        {
        }

        public override void OnApplicationShutdown(ApplicationShutdownContext context)
        {
        }
    }
}