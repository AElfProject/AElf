using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool.Application;

namespace AElf.WebApp.Application.Chain;

public interface ITransactionResultProxyService
{
    ITransactionPoolService TransactionPoolService { get; }
    ITransactionResultQueryService TransactionResultQueryService { get; }
    ITransactionInvalidResultService TransactionInvalidResultService { get; }
}

public class TransactionResultProxyService : ITransactionResultProxyService
{
    public TransactionResultProxyService(ITransactionPoolService transactionPoolService,
        ITransactionResultQueryService transactionResultQueryService, 
        ITransactionInvalidResultService transactionInvalidResultService)
    {
        TransactionPoolService = transactionPoolService;
        TransactionResultQueryService = transactionResultQueryService;
        TransactionInvalidResultService = transactionInvalidResultService;
    }

    public ITransactionPoolService TransactionPoolService { get; set; }
    public ITransactionResultQueryService TransactionResultQueryService { get; set; }
    public ITransactionInvalidResultService TransactionInvalidResultService { get; }
}