using AElf.Configuration.Config.RPC;
using AElf.Kernel;
using AElf.Modularity;
using AElf.Network;
using AElf.RPC.Hubs.Net;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.RPC
{
    [DependsOn(typeof(KernelAElfModule),
        //TODO: remove it
        typeof(NetworkAElfModule))]
    public class RpcAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            //TODO! RPC server should change to controller
            
            context.Services.AddSingleton<IRpcServer,RpcServer>();


            context.Services.AddSingleton<IServiceCollection>(context.Services);


            //TODO: should remove
            context.Services.AddScoped<NetContext>();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            
            
            var rpc = context.ServiceProvider.GetService<IRpcServer>();

            //TODO! change the implement of rpc server.
            rpc.Init(context.ServiceProvider, RpcConfig.Instance.Host, RpcConfig.Instance.Port);

            AsyncHelper.RunSync(rpc.StartAsync);
        }

        public override void OnApplicationShutdown(ApplicationShutdownContext context)
        {
            var rpc = context.ServiceProvider.GetService<IRpcServer>();

            AsyncHelper.RunSync(rpc.StopAsync);
        }
    }
}