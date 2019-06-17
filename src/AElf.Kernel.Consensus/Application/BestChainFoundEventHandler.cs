using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using Volo.Abp.EventBus;

namespace AElf.Kernel.Consensus.Application
{
    /// <summary>
    /// Trigger consensus to update mining scheduler.
    /// </summary>
    public class BestChainFoundEventHandler : ILocalEventHandler<BestChainFoundEvent>
    {
        private readonly IConsensusService _consensusService;

        public BestChainFoundEventHandler(IConsensusService consensusService)
        {
            _consensusService = consensusService;
        }

        public async Task HandleEventAsync(BestChainFoundEvent eventData)
        {
            await _consensusService.TriggerConsensusAsync(new ChainContext
            {
                BlockHash = eventData.BlockHash,
                BlockHeight = eventData.BlockHeight
            });
        }
    }
}