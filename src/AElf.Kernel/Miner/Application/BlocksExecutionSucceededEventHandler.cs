using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Events;

namespace AElf.Kernel.Miner.Application;

public class BlocksExecutionSucceededEventHandler : ILocalEventHandler<BlocksExecutionSucceededEvent>,
    ITransientDependency
{
    private readonly ILogEventProcessingService<IBlocksExecutionSucceededLogEventProcessor> _logEventProcessingService;

    public BlocksExecutionSucceededEventHandler(
        ILogEventProcessingService<IBlocksExecutionSucceededLogEventProcessor> logEventProcessingService)
    {
        _logEventProcessingService = logEventProcessingService;
    }

    public async Task HandleEventAsync(BlocksExecutionSucceededEvent eventData)
    {
        await _logEventProcessingService.ProcessAsync(eventData.BlockExecutedSets);
    }
}