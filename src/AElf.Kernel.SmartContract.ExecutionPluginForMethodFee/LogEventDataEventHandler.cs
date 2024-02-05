using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.SmartContract.Events;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee;

internal class LogEventDataEventHandler : ILocalEventHandler<LogEventDataEvent>, ITransientDependency
{
    private readonly ITransactionSizeFeeSymbolsProvider _transactionSizeFeeSymbolsProvider;
    public ILogger<LogEventDataEventHandler> _logger;

    public LogEventDataEventHandler(ITransactionSizeFeeSymbolsProvider transactionSizeFeeSymbolsProvider,
        ILogger<LogEventDataEventHandler> logger)
    {
        _transactionSizeFeeSymbolsProvider = transactionSizeFeeSymbolsProvider;
        _logger = logger;
    }

    public async Task HandleEventAsync(LogEventDataEvent logEvent)
    {
        _logger.LogDebug("LogEventDataEvent Handler BlockHeight {BlockHeight} , LogEventName {LogEventName}",
            logEvent.Block.Height, logEvent.LogEvent.Name);
        if (logEvent.LogEvent.Name != nameof(ExtraTokenListModified)) return;
        var eventData = new ExtraTokenListModified();
        eventData.MergeFrom(logEvent.LogEvent);
        if (eventData.SymbolListToPayTxSizeFee == null)
            return;

        var transactionSizeFeeSymbols = new TransactionSizeFeeSymbols();
        foreach (var symbolToPayTxSizeFee in eventData.SymbolListToPayTxSizeFee.SymbolsToPayTxSizeFee)
            transactionSizeFeeSymbols.TransactionSizeFeeSymbolList.Add(new TransactionSizeFeeSymbol
            {
                TokenSymbol = symbolToPayTxSizeFee.TokenSymbol,
                AddedTokenWeight = symbolToPayTxSizeFee.AddedTokenWeight,
                BaseTokenWeight = symbolToPayTxSizeFee.BaseTokenWeight
            });

        await _transactionSizeFeeSymbolsProvider.SetTransactionSizeFeeSymbolsAsync(new BlockIndex
        {
            BlockHash = logEvent.Block.GetHash(),
            BlockHeight = logEvent.Block.Height
        }, transactionSizeFeeSymbols);
    }
}