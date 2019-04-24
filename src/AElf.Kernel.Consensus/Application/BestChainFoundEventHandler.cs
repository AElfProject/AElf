using System;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.EventMessages;
using AElf.Kernel.Miner.Application;
using Volo.Abp.EventBus;

namespace AElf.Kernel.Consensus.Application
{
    public class BestChainFoundEventHandler : ILocalEventHandler<BestChainFoundEventData>
    {
        private readonly IConsensusService _consensusService;

        public BestChainFoundEventHandler(IConsensusService consensusService)
        {
            _consensusService = consensusService;
        }

        public async Task HandleEventAsync(BestChainFoundEventData eventData)
        {
            await _consensusService.TriggerConsensusAsync(new ChainContext
            {
                BlockHash = eventData.BlockHash,
                BlockHeight = eventData.BlockHeight
            });
        }
    }
}