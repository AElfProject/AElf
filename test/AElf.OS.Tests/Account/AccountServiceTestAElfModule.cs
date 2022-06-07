using AElf.Kernel.Account.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using AccountService = AElf.OS.Account.Application.AccountService;

namespace AElf.OS.Account;

[DependsOn(typeof(OSTestAElfModule))]
public class AccountServiceTestAElfModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<IAccountService, AccountService>();
    }
}