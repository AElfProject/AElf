using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs3
{
    public class NewIrreversibleBlockFoundEventHandler : ILocalEventHandler<NewIrreversibleBlockFoundEvent>,
        ITransientDependency
    {
        private readonly IProposalService _proposalService;

        public NewIrreversibleBlockFoundEventHandler(IProposalService proposalService)
        {
            _proposalService = proposalService;
        }

        public async Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            await _proposalService.ClearProposalByLibAsync(eventData.BlockHash, eventData.BlockHeight);
        }
    }
}