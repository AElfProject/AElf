using AElf.Contracts.TestKit;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Account.Infrastructure;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

// ReSharper disable InconsistentNaming
namespace AElf.Contracts.TestKet.AEDPoSExtension
{
    public class ContractTestAEDPoSExtensionModule<TSelf> : ContractTestAEDPoSExtensionModule
        where TSelf : ContractTestAEDPoSExtensionModule<TSelf>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<TSelf>();
            base.ConfigureServices(context);
        }
    }

    [DependsOn(
        typeof(ContractTestModule)
    )]
    public class ContractTestAEDPoSExtensionModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IBlockMiningService, BlockMiningService>();
            context.Services.AddSingleton<ITransactionListProvider, TransactionListProvider>();
            context.Services.AddSingleton<IBlockTimeProvider, BlockTimeProvider>();
            context.Services.AddSingleton<IAElfAsymmetricCipherKeyPairProvider, AElfAsymmetricCipherKeyPairProvider>();
            context.Services.AddSingleton<IAccountService, AccountService>();
            context.Services.AddSingleton<IPostExecutionPlugin, ProvideTransactionListPostExecutionPlugin>();
            context.Services.AddSingleton<ITestDataProvider, TestDataProvider>();
            context.Services.AddSingleton<ITransactionTraceProvider, TransactionTraceProvider>();
            context.Services.AddSingleton<TransactionExecutedEventHandler>();

            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        }
    }
}