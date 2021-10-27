using System.Threading.Tasks;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    public class ConsensusValidationFailedEventHandler : ILocalEventHandler<ConsensusValidationFailedEventData>,
        ITransientDependency
    {
        private readonly IConsensusService _consensusService;
        private readonly IBlockchainService _blockchainService;
        public ILogger<ConsensusValidationFailedEventHandler> Logger { get; set; }

        public ConsensusValidationFailedEventHandler(IConsensusService consensusService,
            IBlockchainService blockchainService)
        {
            _consensusService = consensusService;
            _blockchainService = blockchainService;

            Logger = NullLogger<ConsensusValidationFailedEventHandler>.Instance;
        }

        public async Task HandleEventAsync(ConsensusValidationFailedEventData eventData)
        {
            if (eventData.IsReTrigger)
            {
                Logger.LogTrace($"Re-trigger consensus because validation failed.");
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