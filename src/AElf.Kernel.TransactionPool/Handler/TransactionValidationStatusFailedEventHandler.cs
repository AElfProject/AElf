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
        if (!_failStatus.Contains(eventData.TransactionResultStatus)) return; 
        if (!_transactionOptions.StoreInvalidTransactionResultEnabled) return; 
        
        // save to storage
        await _invalidTransactionResultService.AddInvalidTransactionResultsAsync(
            new InvalidTransactionResult
            {
                TransactionId = eventData.TransactionId,
                Status = eventData.TransactionResultStatus,
                Error = TakeErrorMessage(eventData.Error)
            });
    }
    
    private string TakeErrorMessage(string transactionResultError)
    {
        if (string.IsNullOrWhiteSpace(transactionResultError))
            return null;
        using var stringReader = new StringReader(transactionResultError);
        return stringReader.ReadLine();
    }
    
}