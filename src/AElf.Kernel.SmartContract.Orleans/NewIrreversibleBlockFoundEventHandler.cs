using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContract.Grain;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContract.Orleans;

public class NewIrreversibleBlockFoundEventHandler : ILocalEventHandler<NewIrreversibleBlockFoundEvent>,
    ITransientDependency
{
    private readonly ITaskQueueManager _taskQueueManager;
    private readonly ISiloClusterClientContext _siloClusterClientContext;
    public ILocalEventBus LocalEventBus { get; set; }
    public NewIrreversibleBlockFoundEventHandler(ITaskQueueManager taskQueueManager,
        ISiloClusterClientContext siloClusterClientContext)
    {
        _taskQueueManager = taskQueueManager;
        _siloClusterClientContext = siloClusterClientContext;

    }
    public Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
    {
        _taskQueueManager.Enqueue(async () =>
        {
           var grain = _siloClusterClientContext.GetClusterClient().GetGrain<ICleanChainGrain>("CleanCache");
           await grain.CleanCacheAsync(eventData.BlockHeight);
        }, KernelConstants.MergeBlockStateQueueName);
        return Task.CompletedTask;
    }
}