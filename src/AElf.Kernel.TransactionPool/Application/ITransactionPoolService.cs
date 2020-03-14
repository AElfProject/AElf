using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Types;

namespace AElf.Kernel.TransactionPool.Application
{
    
    
    public interface ITransactionPoolService
    {
        Task AddTransactions(IEnumerable<Transaction> transactions);
    }

    public class TransactionPoolService : ITransactionPoolService
    {
        private ITxHub _txHub;

        public TransactionPoolService(ITxHub txHub)
        {
            _txHub = txHub;
        }
        
        //TODO: put tx validation, 
        public Task AddTransactions(IEnumerable<Transaction> transactions)
        {
            
            //_txHub.HandleTransactionsReceivedAsync()
            throw new System.NotImplementedException();
        }
    }
}