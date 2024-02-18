using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Events;
using AElf.Kernel.Token;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee;

internal class LogEventContextHandler : ILocalEventHandler<LogEventContextData>, ITransientDependency
{
    private readonly ITransactionSizeFeeSymbolsProvider _transactionSizeFeeSymbolsProvider;
    private readonly ISmartContractAddressService _smartContractAddressService;

    public LogEventContextHandler(ITransactionSizeFeeSymbolsProvider transactionSizeFeeSymbolsProvider,
        ISmartContractAddressService smartContractAddressService)
    {
        _transactionSizeFeeSymbolsProvider = transactionSizeFeeSymbolsProvider;
        _smartContractAddressService = smartContractAddressService;
    }


    public async Task HandleEventAsync(LogEventContextData eventContextData)
    {
        var smartContractAddressDto = await _smartContractAddressService.GetSmartContractAddressAsync(
            new ChainContext
            {
                BlockHash = eventContextData.Block.GetHash(),
                BlockHeight = eventContextData.Block.Height
            }, TokenSmartContractAddressNameProvider.StringName);
        if (smartContractAddressDto == null) return;
        if (eventContextData.LogEvent.Address != smartContractAddressDto.SmartContractAddress.Address ||
            eventContextData.LogEvent.Name != nameof(ExtraTokenListModified)) return;

        var extraTokenListModified = new ExtraTokenListModified();
        extraTokenListModified.MergeFrom(eventContextData.LogEvent);
        if (extraTokenListModified.SymbolListToPayTxSizeFee == null)
            return;

        var transactionSizeFeeSymbols = new TransactionSizeFeeSymbols();
        foreach (var symbolToPayTxSizeFee in extraTokenListModified.SymbolListToPayTxSizeFee.SymbolsToPayTxSizeFee)
            transactionSizeFeeSymbols.TransactionSizeFeeSymbolList.Add(new TransactionSizeFeeSymbol
            {
                TokenSymbol = symbolToPayTxSizeFee.TokenSymbol,
                AddedTokenWeight = symbolToPayTxSizeFee.AddedTokenWeight,
                BaseTokenWeight = symbolToPayTxSizeFee.BaseTokenWeight
            });

        await _transactionSizeFeeSymbolsProvider.SetTransactionSizeFeeSymbolsAsync(new BlockIndex
        {
            BlockHash = eventContextData.Block.GetHash(),
            BlockHeight = eventContextData.Block.Height
        }, transactionSizeFeeSymbols);
    }
}