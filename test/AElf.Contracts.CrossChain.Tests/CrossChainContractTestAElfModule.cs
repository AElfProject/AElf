using System.Collections.Generic;
using System.Linq;
using AElf.ContractDeployer;
using AElf.ContractTestBase;
using AElf.ContractTestBase.ContractTestKit;
using AElf.GovernmentSystem;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Contracts.CrossChain.Tests
{
    [DependsOn(typeof(ContractTestModule),typeof(AEDPoSAElfModule),
        typeof(TokenKernelAElfModule),
        typeof(GovernmentSystemAElfModule))]
    public class CrossChainContractTestAElfModule : AbpModule
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
            context.Services.RemoveAll<IPreExecutionPlugin>();
            context.Services.AddTransient<IContractDeploymentListProvider, ContractDeploymentListProvider>();
            context.Services.AddTransient<IParliamentContractInitializationDataProvider,
                MainChainParliamentContractInitializationDataProvider>();
            context.Services.AddTransient<IAEDPoSContractInitializationDataProvider,
                MainChainAEDPoSContractInitializationDataProvider>();
            context.Services.AddTransient<ITokenContractInitializationDataProvider,
                MainChainTokenContractInitializationDataProvider>();
            context.Services.RemoveAll(s => s.ImplementationType == typeof(TokenContractInitializationProvider));
            context.Services
                .AddTransient<IContractInitializationProvider, UnitTestTokenContractInitializationProvider>();
            context.Services
                .AddTransient<IContractInitializationProvider, UnitTestCrossChainContractInitializationProvider>();
        }
        
        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var contractCodeProvider = context.ServiceProvider.GetService<IContractCodeProvider>();
            contractCodeProvider.Codes = ContractsDeployer.GetContractCodes<CrossChainContractTestAElfModule>();
        }
    }
}