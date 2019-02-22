using AElf.Contracts.TestBase;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.ChainController.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.SmartContractExecution.Domain;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Consensus.Tests
{
    [DependsOn(
        typeof(ContractTestAElfModule)
    )]
    public class ConsensusContractTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<ConsensusContractTestAElfModule>();

            context.Services.AddSingleton<ITransactionExecutingService, TransactionExecutingService>();
            context.Services.AddSingleton<IBlockchainStateManager, BlockchainStateManager>();
            context.Services.AddSingleton<ISmartContractService, SmartContractService>();
            context.Services.AddSingleton<IChainCreationService, ChainCreationService>();
        }
    }
}