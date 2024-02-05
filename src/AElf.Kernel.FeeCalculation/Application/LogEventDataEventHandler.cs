using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.FeeCalculation.Extensions;
using AElf.Kernel.FeeCalculation.Infrastructure;
using AElf.Kernel.SmartContract.Events;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.FeeCalculation.Application;

public class LogEventDataEventHandler : ILocalEventHandler<LogEventDataEvent>, ITransientDependency
{
    private readonly ICalculateFunctionProvider _calculateFunctionProvider;
    public ILogger<LogEventDataEventHandler> _logger;

    public LogEventDataEventHandler(ICalculateFunctionProvider calculateFunctionProvider,
        ILogger<LogEventDataEventHandler> logger)
    {
        _calculateFunctionProvider = calculateFunctionProvider;
        _logger = logger;
    }

    public async Task HandleEventAsync(LogEventDataEvent logEvent)
    {
        _logger.LogDebug("LogEventDataEvent Handler BlockHeight {BlockHeight} , LogEventName {LogEventName}",
            logEvent.Block.Height, logEvent.LogEvent.Name);
        if (logEvent.LogEvent.Name != nameof(CalculateFeeAlgorithmUpdated)) return;

        var eventData = new CalculateFeeAlgorithmUpdated();
        eventData.MergeFrom(logEvent.LogEvent);
        await _calculateFunctionProvider.AddCalculateFunctions(new BlockIndex
        {
            BlockHash = logEvent.Block.GetHash(),
            BlockHeight = logEvent.Block.Height
        }, eventData.AllTypeFeeCoefficients.ToCalculateFunctionDictionary());
    }
}