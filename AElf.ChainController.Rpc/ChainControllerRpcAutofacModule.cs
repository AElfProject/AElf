using Autofac;

namespace AElf.ChainController.Rpc
{
    public class ChainControllerRpcAutofacModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ChainControllerRpcService>().PropertiesAutowired();
        }
    }
}