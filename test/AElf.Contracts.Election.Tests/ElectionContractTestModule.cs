using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.TestKit;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Election
{
    [DependsOn(typeof(ContractTestModule))]
    public class ElectionContractTestModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            // Justification: Some test cases need to mock current block time.
            context.Services.AddSingleton<ITransactionExecutor, ElectionTransactionExecutor>();
            context.Services.AddSingleton<IBlockValidationService, MockBlockValidationService>();
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        }
    }
}