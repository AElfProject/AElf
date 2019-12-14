using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs1.FreeFeeTransactions;
using AElf.Kernel.Txn.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.ExecutionPluginForProposal
{
    [DependsOn(typeof(SmartContractAElfModule))]
    public class ExecutionPluginForProposalModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<ISystemTransactionGenerator, ProposalApprovalTransactionGenerator>();
            context.Services.AddSingleton<IChargeFeeStrategy, ParliamentContractChargeFeeStrategy>();
            context.Services.AddSingleton<ITransactionValidationProvider, TxHubEntryBannedValidationProvider>();
        }
    }
}