using System;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.EventMessages;
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
        }

        public async Task HandleEventAsync(BlockMiningEventData eventData)
        {
            await _minerService.MineAsync(eventData.ChainId, eventData.PreviousBlockHash, eventData.PreviousBlockHeight,
                eventData.DueTime);
        }
    }
}