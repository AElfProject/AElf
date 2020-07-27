using System.Collections.Generic;
using System.Linq;
using AElf.ContractDeployer;
using AElf.ContractTestBase;
using AElf.ContractTestBase.ContractTestKit;
using AElf.CrossChain;
using AElf.CrossChain.Application;
using AElf.EconomicSystem;
using AElf.GovernmentSystem;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Contracts.MultiToken
{
     [DependsOn(typeof(ContractTestModule),typeof(AEDPoSAElfModule),
        typeof(TokenKernelAElfModule),
        typeof(CrossChainCoreModule),
        typeof(GovernmentSystemAElfModule),
        typeof(EconomicSystemAElfModule))]
    public class MultiTokenContractCrossChainTestAElfModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ConsensusOptions>(options =>
            {
                options.MiningInterval = 4000;
                options.InitialMinerList =
                    SampleAccount.Accounts.Take(5).Select(a => a.KeyPair.PublicKey.ToHex()).ToList();
            });
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
            var instance = new TestTokenBalanceTransactionGenerator();
            context.Services.AddSingleton(instance);
            context.Services.AddSingleton<ISystemTransactionGenerator>(instance);
            context.Services.AddSingleton<IResetBlockTimeProvider, ResetBlockTimeProvider>();
            context.Services.RemoveAll<IPreExecutionPlugin>();
            context.Services.AddTransient<IContractDeploymentListProvider, MultiChainContractDeploymentListProvider>();
            context.Services.AddTransient<IParliamentContractInitializationDataProvider,
                ParliamentContractInitializationDataProvider>();
            context.Services.AddTransient<IAEDPoSContractInitializationDataProvider,
                MainChainAEDPoSContractInitializationDataProvider>();
            context.Services.AddTransient<ITokenContractInitializationDataProvider,
                MainChainTokenContractInitializationDataProvider>();
            context.Services.AddTransient<ICrossChainContractInitializationDataProvider,
                MainChainCrossChainContractInitializationDataProvider>();
            context.Services.RemoveAll(s =>
                s.ImplementationType == typeof(TokenContractInitializationProvider));
            context.Services.RemoveAll(s =>
                s.ImplementationType == typeof(SideChainUnitTestTokenContractInitializationProvider));
            context.Services.RemoveAll(s => s.ImplementationType == typeof(ConsensusTransactionGenerator));

            Configure<HostSmartContractBridgeContextOptions>(options =>
            {
                options.ContextVariables[ContextVariableDictionary.NativeSymbolName] = "ELF";
                options.ContextVariables["SymbolListToPayTxFee"] = "WRITE,READ,STORAGE,TRAFFIC,";
                options.ContextVariables["SymbolListToPayRental"] = "CPU,RAM,DISK,NET";
            });
        }
        
        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var contractCodeProvider = context.ServiceProvider.GetService<IContractCodeProvider>();
            contractCodeProvider.Codes = ContractsDeployer.GetContractCodes<MultiTokenContractCrossChainTestAElfModule>();
        }
    }

    [DependsOn(typeof(ContractTestModule),typeof(AEDPoSAElfModule),
        typeof(TokenKernelAElfModule),
        typeof(CrossChainCoreModule),
        typeof(GovernmentSystemAElfModule))]
    public class MultiTokenContractSideChainTestAElfModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            //Configure<ChainOptions>(options => options.ChainId = SideChainInitializationDataProvider.ChainId);
            Configure<ConsensusOptions>(options =>
            {
                options.MiningInterval = 4000;
                options.InitialMinerList =
                    SampleAccount.Accounts.Take(5).Select(a => a.KeyPair.PublicKey.ToHex()).ToList();
            });
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
            var instance = new TestTokenBalanceTransactionGenerator();
            context.Services.AddSingleton(instance);
            context.Services.AddSingleton<ISystemTransactionGenerator>(instance);
            context.Services.RemoveAll<IPreExecutionPlugin>();
            context.Services.AddTransient<ISideChainInitializationDataProvider, SideChainInitializationDataProvider>();
            context.Services.AddTransient<IContractDeploymentListProvider, SideChainContractDeploymentListProvider>();
            context.Services.AddTransient<IParliamentContractInitializationDataProvider,
                ParliamentContractInitializationDataProvider>();
            context.Services.AddTransient<IAEDPoSContractInitializationDataProvider,
                SideChainAEDPoSContractInitializationDataProvider>();
            context.Services.AddTransient<ITokenContractInitializationDataProvider,
                SideChainTokenContractInitializationDataProvider>();
            context.Services.AddTransient<ICrossChainContractInitializationDataProvider,
                SideChainCrossChainContractInitializationDataProvider>();
            context.Services.RemoveAll(s =>
                s.ImplementationType == typeof(UnitTestTokenContractInitializationProvider));
            context.Services.RemoveAll(s =>
                s.ImplementationType == typeof(TokenContractInitializationProvider));
            context.Services.RemoveAll(s => s.ImplementationType == typeof(ConsensusTransactionGenerator));

            Configure<HostSmartContractBridgeContextOptions>(options =>
            {
                options.ContextVariables[ContextVariableDictionary.NativeSymbolName] = "ELF";
                options.ContextVariables["SymbolListToPayTxFee"] = "WRITE,READ,STORAGE,TRAFFIC,";
                options.ContextVariables["SymbolListToPayRental"] = "CPU,RAM,DISK,NET";
            });
        }
        
        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var contractCodeProvider = context.ServiceProvider.GetService<IContractCodeProvider>();
            contractCodeProvider.Codes = ContractsDeployer.GetContractCodes<MultiTokenContractCrossChainTestAElfModule>();
        }
    }
}