using System.Collections.Generic;
using System.Linq;
using AElf.ContractDeployer;
using AElf.ContractTestBase;
using AElf.ContractTestBase.ContractTestKit;
using AElf.EconomicSystem;
using AElf.GovernmentSystem;
using AElf.Kernel.Consensus.AEDPoS;
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
        typeof(GovernmentSystemAElfModule),
        typeof(EconomicSystemAElfModule))]
    public class MultiTokenContractTestAElfModule : AbpModule
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
            context.Services.RemoveAll<IPreExecutionPlugin>();
            context.Services.AddTransient<IContractDeploymentListProvider, ContractDeploymentListProvider>();
            context.Services.AddTransient<IParliamentContractInitializationDataProvider,
                ParliamentContractInitializationDataProvider>();
            context.Services.AddTransient<IAEDPoSContractInitializationDataProvider,
                MainChainAEDPoSContractInitializationDataProvider>();
            context.Services.AddTransient<ITokenContractInitializationDataProvider,
                MainChainTokenContractInitializationDataProvider>();
            context.Services.RemoveAll(s =>
                s.ImplementationType == typeof(UnitTestTokenContractInitializationProvider));
            context.Services.RemoveAll(s =>
                s.ImplementationType == typeof(SideChainUnitTestTokenContractInitializationProvider));
        }
        
        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var contractCodeProvider = context.ServiceProvider.GetService<IContractCodeProvider>();
            contractCodeProvider.Codes = ContractsDeployer.GetContractCodes<MultiTokenContractTestAElfModule>();
        }
    }
}