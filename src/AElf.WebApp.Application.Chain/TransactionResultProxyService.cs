using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool.Application;

namespace AElf.WebApp.Application.Chain
{
    public interface ITransactionResultProxyService
    {
        ITransactionPoolService TransactionPoolService { get; }
        ITransactionResultQueryService TransactionResultQueryService { get; }
    }

    public class TransactionResultProxyService : ITransactionResultProxyService
    {
        public ITransactionPoolService TransactionPoolService { get; set; }
        public ITransactionResultQueryService TransactionResultQueryService { get; set; }

        public TransactionResultProxyService(ITransactionPoolService transactionPoolService,
            ITransactionResultQueryService transactionResultQueryService)
        {
            TransactionPoolService = transactionPoolService;
            TransactionResultQueryService = transactionResultQueryService;
        }
    }
}