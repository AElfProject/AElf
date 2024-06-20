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

namespace AElf.Kernel;

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
            Logger.LogTrace("Begin ConsensusRequestMiningEventHandler.HandleEventAsync");
            var chain = await _blockchainService.GetChainAsync();
            if (eventData.PreviousBlockHash != chain.BestChainHash)
            {
                Logger.LogDebug("Mining canceled because best chain already updated.");
                return;
            }

            try
            {
                var blockExecutedSet = await _miningRequestService.RequestMiningAsync(new ConsensusRequestMiningDto
                {
                    BlockTime = eventData.BlockTime,
                    BlockExecutionTime = eventData.BlockExecutionTime,
                    MiningDueTime = eventData.MiningDueTime,
                    PreviousBlockHash = eventData.PreviousBlockHash,
                    PreviousBlockHeight = eventData.PreviousBlockHeight
                });
                    
                if (blockExecutedSet != null)
                {
                    await _blockchainService.AddBlockAsync(blockExecutedSet.Block);

                    _taskQueueManager.Enqueue(async () => await _blockAttachService.AttachBlockAsync(blockExecutedSet.Block),
                        KernelConstants.UpdateChainQueueName);

                    Logger.LogTrace("Begin publish block mined event.");
                    await LocalEventBus.PublishAsync(new BlockMinedEventData
                    {
                        BlockHeader = blockExecutedSet.Block.Header,
                        Transactions = blockExecutedSet.Transactions
                    });
                    Logger.LogTrace("End publish block mined event.");

                }
                else
                    await TriggerConsensusEventAsync(chain.BestChainHash, chain.BestChainHeight);
            }
            catch (Exception)
            {
                await TriggerConsensusEventAsync(chain.BestChainHash, chain.BestChainHeight);
                throw;
            }
            Logger.LogTrace("End ConsensusRequestMiningEventHandler.HandleEventAsync");
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