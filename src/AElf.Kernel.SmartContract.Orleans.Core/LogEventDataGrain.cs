using AElf.Kernel.SmartContract.Events;
using AElf.Kernel.SmartContract.Orleans.Strategy;
using Orleans;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContract.Orleans;

[CleanCache]
public class LogEventDataGrain : Grain, ILogEventDataGrain
{
    public ILocalEventBus LocalEventBus { get; set; }

    public LogEventDataGrain()
    {
        LocalEventBus = NullLocalEventBus.Instance;
    }

    public async Task ProcessLogEventAsync(LogEventContextData eventContextData)
    {
        await LocalEventBus.PublishAsync(eventContextData);
    }
}