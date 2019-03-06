using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.SmartContract.Application
{
    public class NewIrreversibleBlockFoundEventHandler : ILocalEventHandler<NewIrreversibleBlockFoundEvent>,
        ITransientDependency
    {
        private readonly IBlockchainStateMergingService _blockchainStateMergingService;

        public NewIrreversibleBlockFoundEventHandler(IBlockchainStateMergingService blockchainStateMergingService)
        {
            _blockchainStateMergingService = blockchainStateMergingService;
        }

        public async Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            await _blockchainStateMergingService.MergeBlockStateAsync(eventData.BlockHeight, eventData.BlockHash);
        }
    }
}