using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Events;
using AElf.Standards.ACS1;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.SmartContract.ExecutionPluginForDelayedMethodFee;

internal class TransactionExecutedEventHandler: ILocalEventHandler<TransactionExecutedEventData>,
    ITransientDependency
{
    private readonly IPrimaryTokenFeeService _txFeeService;
    private readonly ITotalDelayedTransactionFeesMapProvider _totalDelayedTransactionFeesMapProvider;
    private readonly IContractReaderFactory<MethodFeeProviderContractContainer.MethodFeeProviderContractStub> _contractReaderFactory;

    public TransactionExecutedEventHandler(IPrimaryTokenFeeService txFeeService,
        ITotalDelayedTransactionFeesMapProvider totalDelayedTransactionFeesMapProvider,
        IContractReaderFactory<MethodFeeProviderContractContainer.MethodFeeProviderContractStub> contractReaderFactory)
    {
        _txFeeService = txFeeService;
        _totalDelayedTransactionFeesMapProvider = totalDelayedTransactionFeesMapProvider;
        _contractReaderFactory = contractReaderFactory;
    }

    public async Task HandleEventAsync(TransactionExecutedEventData eventData)
    {
        if (eventData.TransactionContext.BlockHeight < DelayedMethodFeeConstants.IntervalBlocksCount)
        {
            return;
        }

        var isApplicableToTransaction = eventData.Descriptors.Any(service =>
            service.File.GetIdentity() == "acs12" || service.File.GetIdentity() == "acs1");
        if (!isApplicableToTransaction)
        {
            return;
        }

        var txContext = eventData.TransactionContext;

        var chainContext = new ChainContext
        {
            BlockHash = txContext.PreviousBlockHash,
            BlockHeight = txContext.BlockHeight - 1
        };
        var txSizeFee = await _txFeeService.CalculateFeeAsync(txContext, chainContext);

        var delayedTxFee = new DelayedTransactionFees
        {
            Address = txContext.Transaction.From,
            Symbol = "ELF",
            Amount = txSizeFee
        };

        var map = await _totalDelayedTransactionFeesMapProvider.GetTotalDelayedTransactionFeesMapAsync(chainContext) ??
                  new TotalDelayedTransactionFeesMap
                  {
                      BlockHash = chainContext.BlockHash,
                      BlockHeight = chainContext.BlockHeight
                  };
        map.Fees.Add(delayedTxFee);
        await _totalDelayedTransactionFeesMapProvider.SetTotalDelayedTransactionFeesMapAsync(new BlockIndex
        {
            BlockHash = chainContext.BlockHash,
            BlockHeight = chainContext.BlockHeight
        }, map);
    }
}