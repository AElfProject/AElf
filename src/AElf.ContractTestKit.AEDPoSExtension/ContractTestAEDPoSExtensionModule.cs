using AElf.Kernel.Account.Application;
using AElf.Kernel.Account.Infrastructure;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Modularity;

// ReSharper disable InconsistentNaming
namespace AElf.ContractTestKit.AEDPoSExtension
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
        typeof(ContractTestWithExecutionPluginModule)
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
            context.Services.AddSingleton<IConsensusExtraDataExtractor, AEDPoSExtraDataExtractor>();
            // context.Services.AddSingleton<ISecretSharingService, SecretSharingService>();
            context.Services.AddSingleton<IInValueCache, InValueCache>();
            context.Services.AddSingleton<ITransactionExecutingService, UnitTestPlainTransactionExecutingService>();
            context.Services.AddSingleton<IPlainTransactionExecutingService, UnitTestPlainTransactionExecutingService>();
            context.Services.RemoveAll<IPreExecutionPlugin>();
            context.Services.RemoveAll<ISystemTransactionGenerator>();
            context.Services
                .AddSingleton<IBroadcastPrivilegedPubkeyListProvider, AEDPoSBroadcastPrivilegedPubkeyListProvider>();
            context.Services.AddSingleton<IConsensusExtraDataProvider, ConsensusExtraDataProvider>();

            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false );
        }
    }
}