using System.Threading.Tasks;
using AElf.Kernel;
using AElf.WebApp.Application.Chain.Infrastructure;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.WebApp.Application.Chain.Application;

public class TransactionValidationStatusChangedEventHandler :
    ILocalEventHandler<TransactionValidationStatusChangedEvent>,
    ITransientDependency
{
    private readonly ITransactionResultStatusCacheProvider _transactionResultStatusCacheProvider;

    public TransactionValidationStatusChangedEventHandler(
        ITransactionResultStatusCacheProvider transactionResultStatusCacheProvider)
    {
        _transactionResultStatusCacheProvider = transactionResultStatusCacheProvider;
    }

    public Task HandleEventAsync(TransactionValidationStatusChangedEvent eventData)
    {
        return Task.CompletedTask;
    }
}