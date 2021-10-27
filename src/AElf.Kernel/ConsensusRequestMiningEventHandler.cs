using System;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel
{
    public class ConsensusRequestMiningEventHandler : ILocalEventHandler<ConsensusRequestMiningEventData>,
        ITransientDependency
    {
        private readonly IBlockAttachService _blockAttachService;
        private readonly ITaskQueueManager _taskQueueManager;
        private readonly IBlockchainService _blockchainService;
        private readonly IConsensusService _consensusService;
        private readonly IMiningRequestService _miningRequestService;

        public ILogger<ConsensusRequestMiningEventHandler> Logger { get; set; }

        public ILocalEventBus LocalEventBus { get; set; }

        public ConsensusRequestMiningEventHandler(
            IBlockAttachService blockAttachService,
            ITaskQueueManager taskQueueManager,
            IBlockchainService blockchainService,
            IConsensusService consensusService, IMiningRequestService miningRequestService)
        {
            _blockAttachService = blockAttachService;
            _taskQueueManager = taskQueueManager;
            _blockchainService = blockchainService;
            _consensusService = consensusService;
            _miningRequestService = miningRequestService;

            Logger = NullLogger<ConsensusRequestMiningEventHandler>.Instance;
            LocalEventBus = NullLocalEventBus.Instance;
        }

        public Task HandleEventAsync(ConsensusRequestMiningEventData eventData)
        {
            _taskQueueManager.Enqueue(async () =>
            {
                var chain = await _blockchainService.GetChainAsync();
                if (eventData.PreviousBlockHash != chain.BestChainHash)
                {
                    Logger.LogDebug("Mining canceled because best chain already updated.");
                    return;
                }

                try
                {
                    var block = await _miningRequestService.RequestMiningAsync(new ConsensusRequestMiningDto
                    {
                        BlockTime = eventData.BlockTime,
                        BlockExecutionTime = eventData.BlockExecutionTime,
                        MiningDueTime = eventData.MiningDueTime,
                        PreviousBlockHash = eventData.PreviousBlockHash,
                        PreviousBlockHeight = eventData.PreviousBlockHeight
                    });
                    
                    if (block != null)
                    {
                        await _blockchainService.AddBlockAsync(block);

                        Logger.LogTrace("Before enqueue attach job.");
                        _taskQueueManager.Enqueue(async () => await _blockAttachService.AttachBlockAsync(block),
                            KernelConstants.UpdateChainQueueName);

                        Logger.LogTrace("Before publish block.");

                        await LocalEventBus.PublishAsync(new BlockMinedEventData
                        {
                            BlockHeader = block.Header,
                        });
                    }
                    else
                        await TriggerConsensusEventAsync(chain.BestChainHash, chain.BestChainHeight);
                }
                catch (Exception)
                {
                    await TriggerConsensusEventAsync(chain.BestChainHash, chain.BestChainHeight);
                    throw;
                }
            }, KernelConstants.ConsensusRequestMiningQueueName);

            return Task.CompletedTask;
        }

        private async Task TriggerConsensusEventAsync(Hash blockHash, long blockHeight)
        {
            await _consensusService.TriggerConsensusAsync(new ChainContext
            {
                BlockHash = blockHash,
                BlockHeight = blockHeight
            });
        }
    }
}