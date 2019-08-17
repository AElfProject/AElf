using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using Microsoft.Extensions.Logging;
using Volo.Abp.EventBus;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    public class ConsensusValidationFailedEventHandler : ILocalEventHandler<ConsensusValidationFailedEventData>
    {
        private readonly IConsensusService _consensusService;
        private readonly IBlockchainService _blockchainService;
        public ILogger<ConsensusValidationFailedEventHandler> Logger { get; set; }

        public ConsensusValidationFailedEventHandler(IConsensusService consensusService, IBlockchainService blockchainService)
        {
            _consensusService = consensusService;
            _blockchainService = blockchainService;
        }

        public async Task HandleEventAsync(ConsensusValidationFailedEventData eventData)
        {
            if (eventData.ValidationResultMessage == "Time slot already passed before execution.")
            {
                var chain = await _blockchainService.GetChainAsync();
                await _consensusService.TriggerConsensusAsync(new ChainContext
                {
                    BlockHash = chain.BestChainHash,
                    BlockHeight = chain.BestChainHeight
                });
            }
        }
    }
}