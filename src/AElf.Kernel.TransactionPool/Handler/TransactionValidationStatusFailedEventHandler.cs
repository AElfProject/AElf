using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.TransactionPool.Application;
using AElf.Types;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.TransactionPool.Handler;

public class TransactionValidationStatusFailedEventHandler :
    ILocalEventHandler<TransactionValidationStatusChangedEvent>,
    ITransientDependency
{
    private readonly IEnumerable<TransactionResultStatus> _failStatus = new List<TransactionResultStatus>
    {
        TransactionResultStatus.Failed, TransactionResultStatus.NodeValidationFailed, TransactionResultStatus.Conflict
    };

    private readonly IInvalidTransactionResultService _invalidTransactionResultService;
    private readonly TransactionOptions _transactionOptions;

    public TransactionValidationStatusFailedEventHandler(
        IOptionsMonitor<TransactionOptions> transactionOptionsMonitor,
        IInvalidTransactionResultService invalidTransactionResultService)
    {
        _invalidTransactionResultService = invalidTransactionResultService;
        _transactionOptions = transactionOptionsMonitor.CurrentValue;
    }

    public async Task HandleEventAsync(TransactionValidationStatusChangedEvent eventData)
    {
        if (!_failStatus.Contains(eventData.TransactionResultStatus) ||
            !_transactionOptions.StoreInvalidTransactionResultEnabled) return;

        // save to storage
        await _invalidTransactionResultService.AddInvalidTransactionResultAsync(
            new InvalidTransactionResult
            {
                TransactionId = eventData.TransactionId,
                Status = eventData.TransactionResultStatus,
                Error = eventData.Error
            });
    }
}