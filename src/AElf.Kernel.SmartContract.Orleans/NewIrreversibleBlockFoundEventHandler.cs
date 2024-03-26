using System.Diagnostics;
using AElf.Kernel.Blockchain.Events;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
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
    private readonly ActivitySource _activitySource;

    public NewIrreversibleBlockFoundEventHandler(ITaskQueueManager taskQueueManager,
        ISiloClusterClientContext siloClusterClientContext,
        ILogger<NewIrreversibleBlockFoundEventHandler> logger,
        Instrumentation instrumentation)
    {
        _taskQueueManager = taskQueueManager;
        _siloClusterClientContext = siloClusterClientContext;
        _logger = logger;
        _activitySource = instrumentation.ActivitySource;
    }

    public async Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
    {
        using var activity = _activitySource.StartActivity();

        var managementGrain = _siloClusterClientContext.GetClusterClient().GetGrain<IManagementGrain>(0);
        var siloHosts = await managementGrain.GetHosts();
        var activeSiloCount = siloHosts?.Where(dic => dic.Value == SiloStatus.Active).ToList().Count;
        _logger.LogDebug("received block height: {0}, active silo count: {1}",
            eventData.BlockHeight, activeSiloCount);
        _taskQueueManager.Enqueue(async () =>
        {
            var tasks = new List<Task>();
            for (var i = 0; i < activeSiloCount; i++)
            {
                var id = i;
                tasks.Add(Task.Run(() =>
                {
                    var grain = _siloClusterClientContext.GetClusterClient().GetGrain<ICleanCacheGrain>(id);
                    return grain.CleanCacheAsync(eventData.BlockHeight);
                }));
            }

            await Task.WhenAll(tasks);
        }, KernelConstants.MergeBlockStateQueueName);
    }
}