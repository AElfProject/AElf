using AElf.Kernel.Account.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.OS.Account
{
    [DependsOn(typeof(OSTestAElfModule))]
    public class AccountServiceTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<IAccountService, OS.Account.Application.AccountService>();
        }
    }
}