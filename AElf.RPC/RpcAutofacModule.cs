using Autofac;

namespace AElf.RPC
{
    public class RpcAutofacModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<RpcServer>().As<IRpcServer>().SingleInstance();
        }
    }
}