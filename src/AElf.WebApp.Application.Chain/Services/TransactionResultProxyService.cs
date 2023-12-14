using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool.Application;

namespace AElf.WebApp.Application.Chain;

public interface ITransactionResultProxyService
{
    ITransactionPoolService TransactionPoolService { get; }
    ITransactionResultQueryService TransactionResultQueryService { get; }
    IInvalidTransactionResultService InvalidTransactionResultService { get; }
}

public class TransactionResultProxyService : ITransactionResultProxyService
{
    public TransactionResultProxyService(ITransactionPoolService transactionPoolService,
        ITransactionResultQueryService transactionResultQueryService, 
        IInvalidTransactionResultService invalidTransactionResultService)
    {
        TransactionPoolService = transactionPoolService;
        TransactionResultQueryService = transactionResultQueryService;
        InvalidTransactionResultService = invalidTransactionResultService;
    }

    public ITransactionPoolService TransactionPoolService { get; set; }
    public ITransactionResultQueryService TransactionResultQueryService { get; set; }
    public IInvalidTransactionResultService InvalidTransactionResultService { get; }
}