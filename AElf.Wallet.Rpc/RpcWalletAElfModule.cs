using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Wallet.Rpc
{
    public class RpcWalletAElfModule:AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {

            context.Services.AddTransient<WalletRpcService>();
        }

    }
}