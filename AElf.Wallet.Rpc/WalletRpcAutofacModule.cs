using Autofac;

namespace AElf.Wallet.Rpc
{
    public class WalletRpcAutofacModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<WalletRpcService>().PropertiesAutowired();
        }
    }
}