using Autofac;

namespace AElf.Net.Rpc
{
    public class NetRpcAutofacModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<NetRpcService>().PropertiesAutowired();
        }
    }
}