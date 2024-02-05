using AElf.Kernel.SmartContract.Events;
using AElf.Kernel.SmartContract.Orleans.Strategy;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContract.Orleans;

[CleanCache]
public class LogEventDataGrain : Grain, ILogEventDataGrain
{
    public ILocalEventBus LocalEventBus { get; set; }
    public ILogger<LogEventDataGrain> _logger;

    public LogEventDataGrain(ILogger<LogEventDataGrain> logger)
    {
        LocalEventBus = NullLocalEventBus.Instance;
        _logger = logger;
    }

    public async Task SubscribeLogEventDataEventAsync(LogEventDataEvent eventData)
    {
        _logger.LogDebug("LogEventDataGrain Handle BlockHeight {BlockHeight} , LogEventName {LogEventName}",
            eventData.Block.Height, eventData.LogEvent.Name);
        await LocalEventBus.PublishAsync(eventData);
    }
}