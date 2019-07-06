using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Contracts.AEDPoSTestBase
{
    public class TransactionListProvider : ITransactionListProvider
    {
        private readonly List<Transaction> _transactionList = new List<Transaction>();

        public Task AddTransactionAsync(Transaction transaction)
        {
            _transactionList.Add(transaction);
            return Task.CompletedTask;
        }

        public Task<List<Transaction>> GetTransactionListAsync()
        {
            return Task.FromResult(_transactionList);
        }

        public Task ResetAsync()
        {
            _transactionList.Clear();
            return Task.CompletedTask;
        }
    }
}