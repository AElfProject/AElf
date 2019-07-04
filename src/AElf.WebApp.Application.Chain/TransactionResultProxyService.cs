using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool.Infrastructure;

namespace AElf.WebApp.Application.Chain
{
    public interface ITransactionResultProxyService
    {
        ITxHub TxHub { get; }
        ITransactionResultQueryService TransactionResultQueryService { get; }
    }

    public class TransactionResultProxyService : ITransactionResultProxyService
    {
        public ITxHub TxHub { get; set; }
        public ITransactionResultQueryService TransactionResultQueryService { get; set; }

        public TransactionResultProxyService(ITxHub txHub, ITransactionResultQueryService transactionResultQueryService)
        {
            TxHub = txHub;
            TransactionResultQueryService = transactionResultQueryService;
        }
    }
}