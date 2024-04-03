using AElf.Kernel.SmartContract.Events;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.SmartContract.Orleans;

public class LogEventContextHandler : ILocalEventHandler<LogEventContextData>, ITransientDependency
{
    private readonly ISiloClusterClientContext _siloClusterClientContext;

    public LogEventContextHandler(ISiloClusterClientContext siloClusterClientContext)
    {
        _siloClusterClientContext = siloClusterClientContext;
    }

    public async Task HandleEventAsync(LogEventContextData eventContextData)
    {
        var managementGrain = _siloClusterClientContext.GetClusterClient().GetGrain<IManagementGrain>(0);
        var siloHosts = await managementGrain.GetHosts();
        var activeSiloCount = siloHosts?.Where(dic => dic.Value == SiloStatus.Active).ToList().Count;

        var tasks = new List<Task>();
        for (var i = 0; i < activeSiloCount; i++)
        {
            var id = i;
            tasks.Add(Task.Run(() =>
            {
                var grain = _siloClusterClientContext.GetClusterClient().GetGrain<ILogEventDataGrain>(id);
                return grain.ProcessLogEventAsync(eventContextData);
            }));
        }

        await Task.WhenAll(tasks);
        
    }
}