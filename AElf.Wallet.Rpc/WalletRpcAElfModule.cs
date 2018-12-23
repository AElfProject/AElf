using AElf.Configuration.Config.RPC;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Wallet.Rpc
{
    public class WalletRpcAElfModule:AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {

            context.Services.AddTransient<WalletRpcService>();
        }

    }
}