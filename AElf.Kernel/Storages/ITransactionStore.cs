using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;

namespace AElf.Kernel.Storages
{
    public interface ITransactionStore
    {
        Task InsertAsync(Transaction tx);
        Task<Transaction> GetAsync(Hash hash);
    }
    
    /// <summary>
    /// Simply use a dictionary to store transactions.
    /// </summary>
    public class TransactionStore : ITransactionStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;

        public TransactionStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }

        public async Task InsertAsync(Transaction tx)
        {
            await _keyValueDatabase.SetAsync(tx.GetHash(), tx);
        }

        public async Task<Transaction> GetAsync(Hash hash)
        {
            return (Transaction) await _keyValueDatabase.GetAsync(hash,typeof(Transaction));
        }
    }
}