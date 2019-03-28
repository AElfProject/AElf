using System;
using System.Threading.Tasks;
using AElf.Kernel.EventMessages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.Miner.Application
{
    public class BlockMiningEventHandler : ILocalEventHandler<BlockMiningEventData>, ITransientDependency
    {
        public readonly IMinerService _minerService;

        public BlockMiningEventHandler(IMinerService minerService)
        {
            _minerService = minerService;
            Logger = NullLogger<BlockMiningEventHandler>.Instance;
        }

        public ILogger<BlockMiningEventHandler> Logger { get; set; }

        public async Task HandleEventAsync(BlockMiningEventData eventData)
        {
            try
            {
                await _minerService.MineAsync(eventData.PreviousBlockHash, eventData.PreviousBlockHeight,
                    eventData.DueTime);
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
                throw;
            }
        }
    }
}