using AElf.Configuration.Config.RPC;
using AElf.Kernel;
using AElf.Modularity;
using AElf.Network;
using AElf.RPC.Hubs.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;
using Volo.Abp.AspNetCore.Modularity;
namespace AElf.RPC
{
    [DependsOn(
        typeof(Volo.Abp.AspNetCore.AbpAspNetCoreModule),
        typeof(KernelAElfModule),
        //TODO: remove it
        typeof(NetworkAElfModule))]
    public class RpcAElfModule: AElfModule
    {

        private IServiceCollection _serviceCollection=null;
        public override void ConfigureServices(ServiceConfigurationContext context)
        {

            var services = context.Services;
            
            services.AddCors();

            services.AddSignalRCore();
            services.AddSignalR();
            
            context.Services.AddScoped<NetContext>();
        }

        public override void PostConfigureServices(ServiceConfigurationContext context)
        {
            RpcServerHelpers.ConfigureServices(context.Services);

            _serviceCollection = context.Services;
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();
            
            RpcServerHelpers.Configure(app,_serviceCollection);

            
            app.UseCors(builder => { builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod(); });
            app.UseSignalR(routes => { routes.MapHub<NetworkHub>("/events/net"); });

        }

        public override void OnPostApplicationInitialization(ApplicationInitializationContext context)
        {
            context.ServiceProvider.GetRequiredService<NetContext>();
        }

        public override void OnApplicationShutdown(ApplicationShutdownContext context)
        {
            
        }
    }
}