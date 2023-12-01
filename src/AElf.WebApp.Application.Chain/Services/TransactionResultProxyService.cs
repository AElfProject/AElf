using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool.Application;
using AElf.WebApp.Application.Chain.Services;

namespace AElf.WebApp.Application.Chain;

public interface ITransactionResultProxyService
{
    ITransactionPoolService TransactionPoolService { get; }
    ITransactionResultQueryService TransactionResultQueryService { get; }
    ITransactionFailedResultService TransactionFailedResultService { get; }
}

public class TransactionResultProxyService : ITransactionResultProxyService
{
    public TransactionResultProxyService(ITransactionPoolService transactionPoolService,
        ITransactionResultQueryService transactionResultQueryService, 
        ITransactionFailedResultService transactionFailedResultService)
    {
        TransactionPoolService = transactionPoolService;
        TransactionResultQueryService = transactionResultQueryService;
        TransactionFailedResultService = transactionFailedResultService;
    }

    public ITransactionPoolService TransactionPoolService { get; set; }
    public ITransactionResultQueryService TransactionResultQueryService { get; set; }
    public ITransactionFailedResultService TransactionFailedResultService { get; }
}