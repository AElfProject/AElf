using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool;
using AElf.Types;
using AElf.WebApp.Application.Chain.Infrastructure;
using AElf.WebApp.Application.Chain.Services;
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
    private readonly ITransactionFailedResultService _transactionFailedResultService;
    private readonly WebAppOptions _webAppOptions;
    private readonly TransactionOptions _transactionOptions;

    public TransactionValidationStatusChangedEventHandler(
        ITransactionResultStatusCacheProvider transactionResultStatusCacheProvider,
        ITransactionResultService transactionResultService,
        IOptionsMonitor<WebAppOptions> optionsSnapshot, 
        IOptionsMonitor<TransactionOptions> transactionOptions, 
        ITransactionFailedResultService transactionFailedResultService)
    {
        _transactionResultStatusCacheProvider = transactionResultStatusCacheProvider;
        _transactionResultService = transactionResultService;
        _transactionFailedResultService = transactionFailedResultService;
        _webAppOptions = optionsSnapshot.CurrentValue;
        _transactionOptions = transactionOptions.CurrentValue;
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

        if (_transactionOptions.SaveFailedResult)
        {
            // save to storage
            _transactionFailedResultService.AddFailedTransactionResultsAsync(eventData.TransactionId,
                new TransactionFailedResult
                {
                    Status = eventData.TransactionResultStatus,
                    Error = TransactionErrorResolver.TakeErrorMessage(eventData.Error, _webAppOptions.IsDebugMode)
                });
        }

        return Task.CompletedTask;
    }
}