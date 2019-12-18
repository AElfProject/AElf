using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.Consensus.Application
{
    /// <summary>
    /// Trigger consensus to update mining scheduler.
    /// </summary>
    public class ConsensusBestChainFoundEventHandler : ILocalEventHandler<BestChainFoundEventData>, ITransientDependency
    {
        private readonly IConsensusService _consensusService;

        public ConsensusBestChainFoundEventHandler(IConsensusService consensusService)
        {
            _consensusService = consensusService;
        }

        /// <summary>
        /// Trigger consensus mining process after event BestChainFoundEventData published by EventBus.
        /// </summary>
        /// <param name="eventData"></param>
        /// <returns></returns>
        public Task HandleEventAsync(BestChainFoundEventData eventData)
        {
            _consensusService.TriggerConsensusAsync(new ChainContext
            {
                BlockHash = eventData.BlockHash,
                BlockHeight = eventData.BlockHeight
            });

            return Task.CompletedTask;
        }
    }
}