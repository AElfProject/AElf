using AElf.Kernel.Account.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel;

[DependsOn(
    typeof(KernelCoreTestAElfModule))]
public class AccountTestAElfModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
        services.AddSingleton<IAccountService, AccountService>();
    }
}