using System;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.Consensus.Events;
using AElf.Kernel.SmartContractExecution;
using AElf.Kernel.SmartContractExecution.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.Miner.Application
{
    public class BlockMiningEventHandler : ILocalEventHandler<ConsensusRequestMiningEventData>, ITransientDependency
    {
        private readonly IMinerService _minerService;
        private readonly IBlockAttachService _blockAttachService;
        private readonly ITaskQueueManager _taskQueueManager;
        public ILogger<BlockMiningEventHandler> Logger { get; set; }

        public BlockMiningEventHandler(IMinerService minerService, IBlockAttachService blockAttachService,
            ITaskQueueManager taskQueueManager)
        {
            _minerService = minerService;
            _blockAttachService = blockAttachService;
            _taskQueueManager = taskQueueManager;
            Logger = NullLogger<BlockMiningEventHandler>.Instance;
        }

        public async Task HandleEventAsync(ConsensusRequestMiningEventData eventData)
        {
            try
            {
                var block = await _minerService.MineAsync(eventData.PreviousBlockHash, eventData.PreviousBlockHeight,
                    eventData.BlockTime, eventData.TimeSpan);
                //TODO: Before attach block should add block signature verify
                _taskQueueManager.Enqueue(async () => await _blockAttachService.AttachBlockAsync(block),
                    KernelConsts.UpdateChainQueueName);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
                throw;
            }
        }
    }
}