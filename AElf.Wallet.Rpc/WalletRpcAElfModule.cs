using AElf.Common.Module;
using AElf.Configuration.Config.RPC;
using Autofac;

namespace AElf.Wallet.Rpc
{
    public class WalletRpcAElfModule:IAElfModlule
    {
        public void Init(ContainerBuilder builder)
        {
            builder.RegisterModule(new WalletRpcAutofacModule());
        }

        public void Run(ILifetimeScope scope)
        {
        }
    }
}