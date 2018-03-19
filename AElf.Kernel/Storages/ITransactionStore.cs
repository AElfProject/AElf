using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;

namespace AElf.Kernel.Storages
{
    public interface ITransactionStore
    {
        Task InsertAsync(ITransaction tx);
        Task<ITransaction> GetAsync(IHash<ITransaction> hash);
    }
    
    /// <summary>
    /// Simply use a dictionary to store transactions.
    /// </summary>
    public class TransactionStore : ITransactionStore
    {
        private static readonly Dictionary<IHash, ITransaction> Transactions = new Dictionary<IHash, ITransaction>();
        
        public Task InsertAsync(ITransaction tx)
        {
            Transactions.Add(new Hash<ITransaction>(tx.CalculateHash()), tx);
            return Task.CompletedTask;
        }

        public Task<ITransaction> GetAsync(IHash<ITransaction> hash)
        {
            foreach (var k in Transactions.Keys)
            {
                if (k.Equals(hash))
                {
                    return Task.FromResult(Transactions[k]);
                }
            }
            throw new InvalidOperationException("Cannot find corresponding transaction.");
        }
    }
    
}