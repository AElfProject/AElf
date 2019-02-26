using System;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.EventMessages;
using AElf.Kernel.Miner.Application;
using Volo.Abp.EventBus;

namespace AElf.Kernel.Consensus.Application
{
    public class BestChainFoundEventHandler : ILocalEventHandler<BestChainFoundEvent>
    {
        private readonly IConsensusService _consensusService;

        public BestChainFoundEventHandler(IConsensusService consensusService)
        {
            _consensusService = consensusService;
        }

        public async Task HandleEventAsync(BestChainFoundEvent eventData)
        {
            await _consensusService.TriggerConsensusAsync(eventData.ChainId);
        }
    }
}