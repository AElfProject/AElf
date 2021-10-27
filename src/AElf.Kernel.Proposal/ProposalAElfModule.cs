using AElf.Kernel.Miner.Application;
using AElf.Kernel.Proposal.Application;
using AElf.Kernel.Proposal.Infrastructure;
using AElf.Kernel.Txn.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Proposal
{
    public class ProposalAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<ISystemTransactionGenerator, ProposalApprovalTransactionGenerator>();
            context.Services.AddSingleton<IProposalProvider, ProposalProvider>();
        }
    }
}