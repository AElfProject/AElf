using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Types;

namespace AElf.Kernel.TransactionPool.Application
{
    public interface ITransactionPoolService
    {
        Task AddTransactionsAsync(IEnumerable<Transaction> transactions);
    }

    public class TransactionPoolService : ITransactionPoolService
    {
        private readonly ITxHub _txHub;
        
        

        public TransactionPoolService(ITxHub txHub)
        {
            _txHub = txHub;
        }

        //TODO: put tx validation, 
        public async Task AddTransactionsAsync(IEnumerable<Transaction> transactions)
        {
            await _txHub.AddTransactionsAsync(
                new TransactionsReceivedEvent() {Transactions = transactions});
        }
    }
}