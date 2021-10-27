using System.Linq;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Account.Infrastructure;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Moq;
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
            context.Services.AddSingleton<IInValueCache, InValueCache>();
            context.Services.AddSingleton<ITransactionExecutingService, UnitTestPlainTransactionExecutingService>();
            context.Services
                .AddSingleton<IPlainTransactionExecutingService, UnitTestPlainTransactionExecutingService>();
            context.Services.RemoveAll<IPreExecutionPlugin>();
            context.Services.RemoveAll<ISystemTransactionGenerator>();
            context.Services
                .AddSingleton<IBroadcastPrivilegedPubkeyListProvider, AEDPoSBroadcastPrivilegedPubkeyListProvider>();
            context.Services.AddSingleton<IConsensusExtraDataProvider, ConsensusExtraDataProvider>();
            context.Services.AddSingleton<IChainTypeProvider, ChainTypeProvider>();

            context.Services.RemoveAll<IHostSmartContractBridgeContext>();
            context.Services.AddTransient(provider =>
            {
                var mockBridgeContext =
                    new Mock<HostSmartContractBridgeContext>(
                            context.Services.GetRequiredServiceLazy<ISmartContractBridgeService>().Value,
                            context.Services.GetRequiredServiceLazy<ITransactionReadOnlyExecutionService>().Value,
                            context.Services.GetRequiredServiceLazy<IAccountService>().Value,
                            context.Services
                                .GetRequiredServiceLazy<IOptionsSnapshot<HostSmartContractBridgeContextOptions>>().Value)
                        .As<IHostSmartContractBridgeContext>();
                mockBridgeContext.CallBase = true;
                mockBridgeContext.Setup(c =>
                        c.GetContractAddressByName(It.IsIn(HashHelper.ComputeFrom("AElf.ContractNames.CrossChain")
                            .ToStorageKey())))
                    .Returns(Address.FromPublicKey(SampleAccount.Accounts.Last().KeyPair.PublicKey));
                return mockBridgeContext.Object;
            });

            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        }
    }
}