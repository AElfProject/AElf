using System;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.EventMessages;
using AElf.Kernel.SmartContractExecution.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.Miner.Application
{
    public class BlockMiningEventHandler : ILocalEventHandler<BlockMiningEventData>, ITransientDependency
    {
        private readonly IMinerService _minerService;
        private readonly IBlockAttachService _blockAttachService;
        public ILogger<BlockMiningEventHandler> Logger { get; set; }

        public BlockMiningEventHandler(IMinerService minerService, IBlockAttachService blockAttachService)
        {
            _minerService = minerService;
            _blockAttachService = blockAttachService;
            Logger = NullLogger<BlockMiningEventHandler>.Instance;
        }

        public async Task HandleEventAsync(BlockMiningEventData eventData)
        {
            try
            {
                var block = await _minerService.MineAsync(eventData.PreviousBlockHash, eventData.PreviousBlockHeight,
                    eventData.DueTime);

                _blockAttachService.AttachBlock(block);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
                throw;
            }
        }
    }
}