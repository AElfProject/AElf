using AElf.Kernel.Node.RPC;
using Autofac;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class RpcServerModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<RpcServer>().As<IRpcServer>();
        }
    }
}