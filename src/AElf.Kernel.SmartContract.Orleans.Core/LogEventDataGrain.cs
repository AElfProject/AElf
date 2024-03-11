using AElf.Kernel.SmartContract.Events;
using AElf.Kernel.SmartContract.Orleans.Strategy;
using Orleans;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContract.Orleans;

[UniformDistribution]
public class LogEventDataGrain : Grain, ILogEventDataGrain
{
    private ILocalEventBus _localEventBus;

    public LogEventDataGrain(ILocalEventBus localEventBus)
    {
        _localEventBus = localEventBus;
    }

    public async Task ProcessLogEventAsync(LogEventContextData eventContextData)
    {
        await _localEventBus.PublishAsync(eventContextData);
    }
}