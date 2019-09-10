using System;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
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

        #pragma warning disable 1998
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

                    if (!ValidateBlockMiningTime(eventData.BlockTime, eventData.MiningDueTime,
                        eventData.BlockExecutionTime))
                    {
                        await TriggerConsensusEventAsync(chain.BestChainHash, chain.BestChainHeight);
                        return;
                    }

                    var blockExecutionDuration =
                        CalculateBlockMiningDuration(eventData.BlockTime, eventData.BlockExecutionTime);

                    Block block;
                    try
                    {
                        block = await _minerService.MineAsync(eventData.PreviousBlockHash,
                            eventData.PreviousBlockHeight, eventData.BlockTime, blockExecutionDuration);
                    }
                    catch (Exception)
                    {
                        await TriggerConsensusEventAsync(chain.BestChainHash, chain.BestChainHeight);
                        throw;
                    }

                    if (TimestampHelper.GetUtcNow() <= eventData.MiningDueTime)
                    {
                        await _blockchainService.AddBlockAsync(block);

                        await LocalEventBus.PublishAsync(new BlockMinedEventData
                        {
                            BlockHeader = block.Header,
//                            HasFork = block.Height <= chain.BestChainHeight
                        });
                        
                        _taskQueueManager.Enqueue(async () => await _blockAttachService.AttachBlockAsync(block),
                            KernelConstants.UpdateChainQueueName);
                    }
                    else
                    {
                        Logger.LogWarning(
                            $"Discard block {block.Height} and trigger once again because mining time slot expired. MiningDueTime : {eventData.MiningDueTime}");
                        await TriggerConsensusEventAsync(chain.BestChainHash, chain.BestChainHeight);
                    }
                }, KernelConstants.ConsensusRequestMiningQueueName);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
                throw;
            }
        }

        private async Task TriggerConsensusEventAsync(Hash blockHash, long blockHeight)
        {
            await _consensusService.TriggerConsensusAsync(new ChainContext
            {
                BlockHash = blockHash,
                BlockHeight = blockHeight
            });
        }

        private bool ValidateBlockMiningTime(Timestamp blockTime, Timestamp miningDueTime,
            Duration blockExecutionDuration)
        {
            if (IsGenesisBlockMining(blockTime))
                return true;
            
            if (miningDueTime < blockTime + blockExecutionDuration)
            {
                Logger.LogWarning(
                    $"Mining canceled because mining time slot expired. MiningDueTime: {miningDueTime}, BlockTime: {blockTime}, Duration: {blockExecutionDuration}");
                return false;
            }
                    
            if (blockTime + blockExecutionDuration < TimestampHelper.GetUtcNow())
            {
                Logger.LogTrace($"Will cancel mining due to timeout: Actual mining time: {blockTime}, " +
                                $"execution limit: {blockExecutionDuration.Milliseconds()} ms.");
                return false;
            }

            return true;
        }

        private Duration CalculateBlockMiningDuration(Timestamp blockTime, Duration expectedDuration)
        {
            if (IsGenesisBlockMining(blockTime))
                return expectedDuration;
            
            return blockTime + expectedDuration - TimestampHelper.GetUtcNow();
        }

        private bool IsGenesisBlockMining(Timestamp blockTime)
        {
            return blockTime < new Timestamp {Seconds = 3600};
        }
    }
}