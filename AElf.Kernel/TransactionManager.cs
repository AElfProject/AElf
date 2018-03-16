﻿using System.Threading.Tasks;
using AElf.Kernel.Storages;

namespace AElf.Kernel
{
    public class TransactionManager: ITransactionManager
    {
        private readonly ITransactionStore _transactionStore;

        public TransactionManager(ITransactionStore transactionStore)
        {
            _transactionStore = transactionStore;
        }

        public async Task AddTransactionAsync(ITransaction tx)
        {
            await _transactionStore.InsertAsync(tx);
        }
    }
}