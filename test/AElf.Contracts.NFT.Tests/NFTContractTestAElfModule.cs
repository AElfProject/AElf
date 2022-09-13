using AElf.ContractDeployer;
using AElf.Contracts.MultiToken;
using AElf.ContractTestBase;
using AElf.ContractTestBase.ContractTestKit;
using AElf.EconomicSystem;
using AElf.GovernmentSystem;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Token;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Contracts.NFT;

[DependsOn(typeof(ContractTestModule), typeof(AEDPoSAElfModule),
    typeof(TokenKernelAElfModule),
    typeof(GovernmentSystemAElfModule),
    typeof(EconomicSystemAElfModule),
    typeof(MultiTokenContractTestAElfModule))]
public class NFTContractTestAElfModule : AbpModule
{
    public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
    {
        var contractCodeProvider = context.ServiceProvider.GetService<IContractCodeProvider>();
        contractCodeProvider.Codes = ContractsDeployer.GetContractCodes<NFTContractTestAElfModule>();
    }
}