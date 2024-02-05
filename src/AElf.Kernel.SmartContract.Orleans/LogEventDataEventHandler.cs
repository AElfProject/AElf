using AElf.Kernel.SmartContract.Events;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.SmartContract.Orleans;

public class LogEventDataEventHandler : ILocalEventHandler<LogEventDataEvent>, ITransientDependency
{
    public ILogger<LogEventDataEventHandler> _logger;
    private readonly ITaskQueueManager _taskQueueManager;
    private readonly ISiloClusterClientContext _siloClusterClientContext;

    public LogEventDataEventHandler(ITaskQueueManager taskQueueManager,
        ISiloClusterClientContext siloClusterClientContext,
        ILogger<LogEventDataEventHandler> logger)
    {
        _taskQueueManager = taskQueueManager;
        _siloClusterClientContext = siloClusterClientContext;
        _logger = logger;
    }

    public async Task HandleEventAsync(LogEventDataEvent eventData)
    {
        var managementGrain = _siloClusterClientContext.GetClusterClient().GetGrain<IManagementGrain>(0);
        var siloHosts = await managementGrain.GetHosts();
        var activeSiloCount = siloHosts?.Where(dic => dic.Value == SiloStatus.Active).ToList().Count;
        _taskQueueManager.Enqueue(async () =>
        {
            var tasks = new List<Task>();
            for (var i = 0; i < activeSiloCount; i++)
            {
                var id = i;
                tasks.Add(Task.Run(() =>
                {
                    var grain = _siloClusterClientContext.GetClusterClient().GetGrain<ILogEventDataGrain>(id);
                    return grain.SubscribeLogEventDataEventAsync(eventData);
                }));
            }

            await Task.WhenAll(tasks);
        }, KernelConstants.MergeBlockStateQueueName);
    }
}