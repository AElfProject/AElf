using System;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;
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
        private readonly IMinerService _minerService;
        private readonly IBlockAttachService _blockAttachService;
        private readonly ITaskQueueManager _taskQueueManager;
        private readonly IBlockchainService _blockchainService;
        private readonly IConsensusService _consensusService;
        public ILogger<ConsensusRequestMiningEventHandler> Logger { get; set; }

        public ILocalEventBus LocalEventBus { get; set; }

        public ConsensusRequestMiningEventHandler(IMinerService minerService, IBlockAttachService blockAttachService,
            ITaskQueueManager taskQueueManager, IBlockchainService blockchainService,
            IConsensusService consensusService)
        {
            _minerService = minerService;
            _blockAttachService = blockAttachService;
            _taskQueueManager = taskQueueManager;
            _blockchainService = blockchainService;
            _consensusService = consensusService;
            Logger = NullLogger<ConsensusRequestMiningEventHandler>.Instance;
            LocalEventBus = NullLocalEventBus.Instance;
        }

        public async Task HandleEventAsync(ConsensusRequestMiningEventData eventData)
        {
            try
            {
                _taskQueueManager.Enqueue(async () =>
                {
                    var chain = await _blockchainService.GetChainAsync();
                    if (eventData.PreviousBlockHash != chain.BestChainHash)
                    {
                        Logger.LogWarning("Mining canceled because best chain already updated.");
                        return;
                    }

                    if (eventData.BlockTime > new Timestamp {Seconds = 3600} &&
                        eventData.BlockTime + eventData.BlockExecutionTime <
                        TimestampHelper.GetUtcNow())
                    {
                        Logger.LogTrace(
                            $"Will cancel mining due to timeout: Actual mining time: {eventData.BlockTime}, " +
                            $"execution limit: {eventData.BlockExecutionTime.Milliseconds()} ms.");

                        await _consensusService.TriggerConsensusAsync(new ChainContext
                        {
                            BlockHash = chain.BestChainHash,
                            BlockHeight = chain.BestChainHeight
                        });
                        return;
                    }

                    var now = TimestampHelper.GetUtcNow();
                    var blockExecutionDuration = now + eventData.BlockExecutionTime < eventData.MiningDueTime
                        ? eventData.BlockExecutionTime
                        : eventData.MiningDueTime - now;

                    var block = await _minerService.MineAsync(eventData.PreviousBlockHash,
                        eventData.PreviousBlockHeight, eventData.BlockTime, blockExecutionDuration);

                    if (TimestampHelper.GetUtcNow() <= eventData.MiningDueTime)
                    {
                        await _blockchainService.AddBlockAsync(block);

                        await LocalEventBus.PublishAsync(new BlockMinedEventData
                        {
                            BlockHeader = block.Header,
                            HasFork = block.Height <= chain.BestChainHeight
                        });

                        // Self mined block do not need do verify
                        _taskQueueManager.Enqueue(async () => await _blockAttachService.AttachBlockAsync(block),
                            KernelConstants.UpdateChainQueueName);
                    }
                }, KernelConstants.ConsensusRequestMiningQueueName);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
                throw;
            }
        }
    }
}