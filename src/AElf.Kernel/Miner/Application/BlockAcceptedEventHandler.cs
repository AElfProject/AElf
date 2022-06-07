using System.Collections.Generic;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContract.Application;

namespace AElf.Kernel.Miner.Application;

public class BlockAcceptedEventHandler : ILocalEventHandler<BlockAcceptedEvent>, ITransientDependency
{
    private readonly ILogEventProcessingService<IBlockAcceptedLogEventProcessor> _logEventProcessingService;

    public BlockAcceptedEventHandler(
        ILogEventProcessingService<IBlockAcceptedLogEventProcessor> logEventProcessingService)
    {
        _logEventProcessingService = logEventProcessingService;
    }

    public async Task HandleEventAsync(BlockAcceptedEvent eventData)
    {
        await _logEventProcessingService.ProcessAsync(new List<BlockExecutedSet> { eventData.BlockExecutedSet });
    }
}