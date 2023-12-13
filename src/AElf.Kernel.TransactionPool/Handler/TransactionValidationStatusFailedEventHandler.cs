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
    private static readonly IEnumerable<TransactionResultStatus> FailStatus = new List<TransactionResultStatus>
    {
        TransactionResultStatus.Failed, TransactionResultStatus.NodeValidationFailed, TransactionResultStatus.Conflict
    };

    private readonly ITransactionInvalidResultService _transactionInvalidResultService;
    private readonly TransactionOptions _transactionOptions;

    public TransactionValidationStatusFailedEventHandler(
        IOptionsMonitor<TransactionOptions> transactionOptionsMonitor, 
        ITransactionInvalidResultService transactionInvalidResultService)
    {
        _transactionInvalidResultService = transactionInvalidResultService;
        _transactionOptions = transactionOptionsMonitor.CurrentValue;
    }

    public Task HandleEventAsync(TransactionValidationStatusChangedEvent eventData)
    {
        if (!FailStatus.Contains(eventData.TransactionResultStatus)) return Task.CompletedTask; 
        if (!_transactionOptions.StoreInvalidTransactionResultEnabled) return Task.CompletedTask; 
        
        // save to storage
        _transactionInvalidResultService.AddFailedTransactionResultsAsync(
            new InvalidTransactionResult
            {
                TransactionId = eventData.TransactionId,
                Status = eventData.TransactionResultStatus,
                Error = TakeErrorMessage(eventData.Error)
            });
        
        return Task.CompletedTask;
    }
    
    public static string TakeErrorMessage(string transactionResultError)
    {
        if (string.IsNullOrWhiteSpace(transactionResultError))
            return null;
        using var stringReader = new StringReader(transactionResultError);
        return stringReader.ReadLine();
    }
    
}