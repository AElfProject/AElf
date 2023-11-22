using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using AElf.WebApp.Application.Chain.Infrastructure;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.WebApp.Application.Chain.Application;

public class TransactionValidationStatusChangedEventHandler :
    ILocalEventHandler<TransactionValidationStatusChangedEvent>,
    ITransientDependency
{
    private readonly ITransactionResultStatusCacheProvider _transactionResultStatusCacheProvider;
    private readonly ITransactionResultService _transactionResultService;
    private readonly WebAppOptions _webAppOptions;

    public TransactionValidationStatusChangedEventHandler(
        ITransactionResultStatusCacheProvider transactionResultStatusCacheProvider,
        ITransactionResultService transactionResultService,
        IOptionsMonitor<WebAppOptions> optionsSnapshot)
    {
        _transactionResultStatusCacheProvider = transactionResultStatusCacheProvider;
        _transactionResultService = transactionResultService;
        _webAppOptions = optionsSnapshot.CurrentValue;
    }

    public Task HandleEventAsync(TransactionValidationStatusChangedEvent eventData)
    {
        
        // save to local cache
        _transactionResultStatusCacheProvider.ChangeTransactionResultStatus(eventData.TransactionId,
            new TransactionValidateStatus
            {
                TransactionResultStatus = eventData.TransactionResultStatus,
                Error = eventData.Error
            });
        
        // save to storage
        _transactionResultService.AddFailedTransactionResultsAsync(new TransactionResult
        {
            TransactionId = eventData.TransactionId,
            Status = eventData.TransactionResultStatus,
            Error = TransactionErrorResolver.TakeErrorMessage(eventData.Error, _webAppOptions.IsDebugMode)
        });
        
        return Task.CompletedTask;
    }
}