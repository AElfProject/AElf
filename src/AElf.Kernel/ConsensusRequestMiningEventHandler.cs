using System;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContractExecution.Application;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

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

        public ConsensusRequestMiningEventHandler(IMinerService minerService, IBlockAttachService blockAttachService,
            ITaskQueueManager taskQueueManager, IBlockchainService blockchainService)
        {
            _minerService = minerService;
            _blockAttachService = blockAttachService;
            _taskQueueManager = taskQueueManager;
            _blockchainService = blockchainService;
            Logger = NullLogger<ConsensusRequestMiningEventHandler>.Instance;
        }

        public async Task HandleEventAsync(ConsensusRequestMiningEventData eventData)
        {
            try
            {
                _taskQueueManager.Enqueue(async () =>
                {
                    if (eventData.BlockTime.ToTimestamp() > new Timestamp {Seconds = 3600} &&
                        eventData.BlockTime.ToTimestamp() + eventData.BlockExecutionTime.ToDuration() <
                        DateTime.UtcNow.ToTimestamp())
                    {
                        Logger.LogTrace(
                            $"Will cancel mining due to timeout: Actual mining time: {eventData.BlockTime.ToTimestamp()}, execution limit: {eventData.BlockExecutionTime.TotalMilliseconds} ms.");
                        return;
                    }

                    var block = await _minerService.MineAsync(eventData.PreviousBlockHash,
                        eventData.PreviousBlockHeight,
                        eventData.BlockTime, eventData.BlockExecutionTime);

                    await _blockchainService.AddBlockAsync(block);

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