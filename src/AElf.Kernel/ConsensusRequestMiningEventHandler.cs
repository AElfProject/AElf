using System;
using System.Threading.Tasks;
using AElf.Kernel.EventMessages;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContractExecution.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel
{
    public class ConsensusRequestMiningEventHandler : ILocalEventHandler<ConsensusRequestMiningEventData>, ITransientDependency
    {
        private readonly IMinerService _minerService;
        private readonly IBlockAttachService _blockAttachService;
        private readonly ITaskQueueManager _taskQueueManager;
        public ILogger<ConsensusRequestMiningEventHandler> Logger { get; set; }

        public ConsensusRequestMiningEventHandler(IMinerService minerService, IBlockAttachService blockAttachService,
            ITaskQueueManager taskQueueManager)
        {
            _minerService = minerService;
            _blockAttachService = blockAttachService;
            _taskQueueManager = taskQueueManager;
            Logger = NullLogger<ConsensusRequestMiningEventHandler>.Instance;
        }

        public async Task HandleEventAsync(ConsensusRequestMiningEventData eventData)
        {
            try
            {
                var block = await _minerService.MineAsync(eventData.PreviousBlockHash, eventData.PreviousBlockHeight,
                    eventData.BlockTime, eventData.TimeSpan);
                // Self mined block do not need do verify
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