using AElf.ChainController.Rpc;
using Autofac;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class RpcServicesModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ChainControllerRpcService>().PropertiesAutowired();
        }
    }
}