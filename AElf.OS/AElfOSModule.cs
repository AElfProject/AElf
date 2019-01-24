using AElf.Common.Application;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Account;
using AElf.Modularity;
using AElf.OS.Account;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.OS
{
    [DependsOn(typeof(KernelAElfModule))]
    public class OSAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            Configure<AccountOptions>(configuration);

            var keyStore = new AElfKeyStore(ApplicationHelpers.ConfigPath);
            context.Services.AddSingleton<AElfKeyStore>(keyStore);
            context.Services.AddTransient<IAccountService, AccountService>();
        }
    }
}