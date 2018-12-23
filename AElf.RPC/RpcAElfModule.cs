using AElf.Configuration.Config.RPC;
using AElf.Kernel;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.RPC
{
    [DependsOn(typeof(KernelAElfModule))]
    public class RpcAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            //TODO! RPC server should change to controller
            
            context.Services.AddSingleton<IRpcServer,RpcServer>();

        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            
            
            var rpc = context.ServiceProvider.GetService<IRpcServer>();
            
            //TODO! change the implement of rpc server.
            //rpc.Init(scope, RpcConfig.Instance.Host, RpcConfig.Instance.Port);
        }

    }
}