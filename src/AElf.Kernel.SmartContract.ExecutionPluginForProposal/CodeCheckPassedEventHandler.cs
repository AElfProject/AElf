using System.Linq;
using Acs3;
using System.Threading.Tasks;
using AElf.Kernel.Proposal.Application;
using AElf.Kernel.SmartContract.Events;
using Volo.Abp.EventBus;

namespace AElf.Kernel.SmartContract.ExecutionPluginForProposal
{
    public class CodeCheckPassedEventHandler : ILocalEventHandler<CodeCheckPassedEvent>
    {
        private readonly IProposalService _proposalService;

        public CodeCheckPassedEventHandler(IProposalService proposalService)
        {
            _proposalService = proposalService;
        }

        public Task HandleEventAsync(CodeCheckPassedEvent eventData)
        {
            var proposalId = ProposalCreated.Parser
                .ParseFrom(eventData.TransactionResult.Logs.First(l => l.Name == nameof(ProposalCreated)).NonIndexed)
                .ProposalId;
            // Cache proposal id to generate system approval transaction later
            _proposalService.AddNotApprovedProposal(proposalId, eventData.TransactionResult.BlockNumber);

            return Task.CompletedTask;
        }
    }
}