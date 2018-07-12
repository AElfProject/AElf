using System;
using System.Threading.Tasks;
using AElf.Kernel.Storages;
using AElf.Kernel.Types;

namespace AElf.Kernel.Managers
{
    public class TransactionManager: ITransactionManager
    {
        private readonly ITransactionStore _transactionStore;

        public TransactionManager(ITransactionStore transactionStore)
        {
            _transactionStore = transactionStore;
        }

        public async Task<Hash> AddTransactionAsync(ITransaction tx)
        {
            Console.WriteLine($"Add tx: {tx.GetHash().ToHex()}");
            return await _transactionStore.InsertAsync(tx);
        }

        public async Task<ITransaction> GetTransaction(Hash txId)
        {
            Console.WriteLine($"Get tx: {txId.ToHex()}");
            return await _transactionStore.GetAsync(txId);
        }
    }
}