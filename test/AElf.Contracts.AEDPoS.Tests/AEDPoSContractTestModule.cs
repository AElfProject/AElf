using System.Linq;
using AElf.Contracts.Deployer;
using AElf.ContractTestBase;
using AElf.ContractTestBase.ContractTestKit;
using AElf.EconomicSystem;
using AElf.GovernmentSystem;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Contracts.AEDPoS
{
    [DependsOn(typeof(ContractTestModule),
        typeof(AEDPoSAElfModule),
        typeof(TokenKernelAElfModule),
        typeof(GovernmentSystemAElfModule))]
    // ReSharper disable once InconsistentNaming
    public class AEDPoSContractTestAElfModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ConsensusOptions>(options =>
            {
                options.MiningInterval = 4000;
                options.InitialMinerList =
                    SampleAccount.Accounts.Take(17).Select(a => a.KeyPair.PublicKey.ToHex()).ToList();
                options.PeriodSeconds = 120;
                options.MinerIncreaseInterval = 240;
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
            context.Services.AddTransient<IContractInitializationProvider, EconomicContractInitializationProvider>();
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var contractCodeProvider = context.ServiceProvider.GetService<IContractCodeProvider>();
            contractCodeProvider.Codes = ContractsDeployer.GetContractCodes<AEDPoSContractTestAElfModule>();
        }
    }
}