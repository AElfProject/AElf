using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography;
using AElf.Kernel.Account.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Modularity;
using AElf.OS;
using AElf.OS.Account;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace AElf.Crosschain.Tests
{
    [DependsOn(typeof(AbpEventBusModule))]
    public class CrosschainTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
//            var stubAccoutOptions = CrosschainTestHelper.FakeAccountOption();
//            context.Services.AddSingleton(provider => stubAccoutOptions);
//            context.Services.AddSingleton(provider => CrosschainTestHelper.FakeKeyStore());
//            context.Services.AddTransient<IAccountService, AccountService>();
//            context.Services.AddSingleton(provider =>
//                CrosschainTestHelper.FakeSmartContractExecutiveService());
        }
    }
}