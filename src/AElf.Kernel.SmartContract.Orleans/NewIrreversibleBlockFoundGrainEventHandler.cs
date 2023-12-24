using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContract.Orleans;

public class NewIrreversibleBlockFoundGrainEventHandler : ILocalEventHandler<NewIrreversibleBlockFoundEvent>,
    ITransientDependency
{
    private readonly ITaskQueueManager _taskQueueManager;
    private readonly ISiloClusterClientContext _siloClusterClientContext;
    private readonly IPlainTransactionExecutingGrainProvider _plainTransactionExecutingGrainProvider;

    public NewIrreversibleBlockFoundGrainEventHandler(ITaskQueueManager taskQueueManager,
        ISiloClusterClientContext siloClusterClientContext,
        IPlainTransactionExecutingGrainProvider plainTransactionExecutingGrainProvider)
    {
        _taskQueueManager = taskQueueManager;
        _siloClusterClientContext = siloClusterClientContext;
        Logger = NullLogger<NewIrreversibleBlockFoundGrainEventHandler>.Instance;
        _plainTransactionExecutingGrainProvider = plainTransactionExecutingGrainProvider;

    }

    public ILogger<NewIrreversibleBlockFoundGrainEventHandler> Logger { get; set; }
    public ILocalEventBus LocalEventBus { get; set; }

    public Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
    {
        Logger.Debug($"NewIrreversibleBlockFoundGrainEventHandler.HandleEventAsync,eventData:{JsonConvert.SerializeObject(eventData)}");
        if (eventData.BlockHeight <= 100)
        {
            return Task.CompletedTask;
        }

        _taskQueueManager.Enqueue(async () =>
        {
           var id = _plainTransactionExecutingGrainProvider.TryGetGrainId(typeof(NewIrreversibleBlockFoundGrainEventHandler).Name, 
               out var pool);
           var grain = _siloClusterClientContext.GetClusterClient().GetGrain<IPlainTransactionCacheClearGrain>(typeof(NewIrreversibleBlockFoundGrainEventHandler).Name + id);
           pool.Add(id);
           await grain.CleanChainAsync(eventData.BlockHeight);
        }, KernelConstants.ChainCleaningQueueName);
        return Task.CompletedTask;
    }
}