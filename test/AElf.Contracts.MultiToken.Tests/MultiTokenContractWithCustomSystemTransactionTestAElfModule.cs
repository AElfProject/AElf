using AElf.Contracts.TestBase;
using AElf.Cryptography;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Account.Infrastructure;
using AElf.Kernel.Miner.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Contracts.MultiToken
{
    [DependsOn(typeof(ContractTestAElfModule))]
    public class MultiTokenContractWithCustomSystemTransactionTestAElfModule : ContractTestAElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<MultiTokenContractWithCustomSystemTransactionTestAElfModule>();
            context.Services.AddTransient<IAccountService, AccountService>();
            context.Services.AddTransient<ISystemTransactionGenerator, TestTokenBalanceTransactionGenerator>();
        }
        
        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var keyPair =
                CryptoHelpers.FromPrivateKey(
                    ByteArrayHelpers.FromHexString(TestTokenBalanceContractTestConstants.PrivateKeyHex));
            context.ServiceProvider.GetService<IAElfAsymmetricCipherKeyPairProvider>()
                .SetKeyPair(keyPair);
        }
    }

    public class TestTokenBalanceContractTestConstants
    {
        public const string PrivateKeyHex = "1594b1526c4bf347058388f93f1430925b7bbb4e0fa053540a58141736717a21";
    }
}