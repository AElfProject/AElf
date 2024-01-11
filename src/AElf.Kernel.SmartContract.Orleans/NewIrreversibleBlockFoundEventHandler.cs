using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContract.Grains;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContract.Orleans;

public class NewIrreversibleBlockFoundEventHandler : ILocalEventHandler<NewIrreversibleBlockFoundEvent>,
    ITransientDependency
{
    public ILogger<NewIrreversibleBlockFoundEventHandler> _logger;
    private readonly ITaskQueueManager _taskQueueManager;
    private readonly ISiloClusterClientContext _siloClusterClientContext;
    public ILocalEventBus LocalEventBus { get; set; }
    public NewIrreversibleBlockFoundEventHandler(ITaskQueueManager taskQueueManager,
        ISiloClusterClientContext siloClusterClientContext,
        ILogger<NewIrreversibleBlockFoundEventHandler> logger)
    {
        _taskQueueManager = taskQueueManager;
        _siloClusterClientContext = siloClusterClientContext;
        _logger = logger;
    }
    public Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
    {
        _logger.LogInformation("NewIrreversibleBlockFoundEventHandler.HandleEventAsync-Silo received block height: {0}",
            eventData.BlockHeight);
        _taskQueueManager.Enqueue(async () =>
        {
           var grain = _siloClusterClientContext.GetClusterClient().GetGrain<ICleanCacheGrain>("CleanCache");
           await grain.CleanCacheAsync(eventData.BlockHeight);
        }, KernelConstants.MergeBlockStateQueueName);
        return Task.CompletedTask;
    }
}