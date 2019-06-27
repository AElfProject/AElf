using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus;
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

        public ILogger<ConsensusRequestMiningEventHandler> Logger { get; set; }
        
        public ILocalEventBus LocalEventBus { get; set; }

        public ConsensusRequestMiningEventHandler(IMinerService minerService, IBlockAttachService blockAttachService,
            ITaskQueueManager taskQueueManager, IBlockchainService blockchainService)
        {
            _minerService = minerService;
            _blockAttachService = blockAttachService;
            _taskQueueManager = taskQueueManager;
            _blockchainService = blockchainService;
            Logger = NullLogger<ConsensusRequestMiningEventHandler>.Instance;
            LocalEventBus = NullLocalEventBus.Instance;
        }

        public async Task HandleEventAsync(ConsensusRequestMiningEventData eventData)
        {
            try
            {
                _taskQueueManager.Enqueue(async () =>
                {
                    if (eventData.BlockTime > new Timestamp {Seconds = 3600} &&
                        eventData.BlockTime + eventData.BlockExecutionTime <
                        TimestampHelper.GetUtcNow())
                    {
                        Logger.LogTrace(
                            $"Will cancel mining due to timeout: Actual mining time: {eventData.BlockTime}, " +
                            $"execution limit: {eventData.BlockExecutionTime.Milliseconds()} ms.");
                    }

                    var block = await _minerService.MineAsync(eventData.PreviousBlockHash,
                        eventData.PreviousBlockHeight,
                        eventData.BlockTime, eventData.BlockExecutionTime);

                    await _blockchainService.AddBlockAsync(block);

                    var chain = await _blockchainService.GetChainAsync();
                    await LocalEventBus.PublishAsync(new BlockMinedEventData()
                    {
                        BlockHeader = block.Header,
                        HasFork = block.Height <= chain.BestChainHeight
                    });

                    // Self mined block do not need do verify
                    _taskQueueManager.Enqueue(async () => await _blockAttachService.AttachBlockAsync(block),
                        KernelConstants.UpdateChainQueueName);
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