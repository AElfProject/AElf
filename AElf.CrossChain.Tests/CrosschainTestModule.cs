using AElf.Kernel.Account.Application;
using AElf.Modularity;
using AElf.OS.Account;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace AElf.CrossChain
{
    [DependsOn(typeof(AbpEventBusModule))]
    public class CrosschainTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var stubAccoutOptions = CrosschainTestHelper.FakeAccountOption();
            context.Services.AddSingleton(provider => stubAccoutOptions);
            context.Services.AddSingleton(provider => CrosschainTestHelper.FakeKeyStore());
            context.Services.AddTransient<IAccountService, AccountService>();
            context.Services.AddSingleton(provider =>
                CrosschainTestHelper.FakeSmartContractExecutiveService());
        }
    }
}