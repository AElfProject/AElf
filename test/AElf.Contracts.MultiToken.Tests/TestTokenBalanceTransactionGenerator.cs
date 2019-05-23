using System;
using System.Collections.Generic;
using AElf.Kernel.Miner.Application;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Contracts.MultiToken
{
    public class TestTokenBalanceTransactionGenerator : ISystemTransactionGenerator, ISingletonDependency
    {
        public Func<Address, long, Hash, Transaction> GenerateTransactionFunc { get; set; }

        public void GenerateTransactions(Address @from, long preBlockHeight, Hash preBlockHash,
            ref List<Transaction> generatedTransactions)
        {
            if (GenerateTransactionFunc == null)
            {
                return;
            }

            generatedTransactions.Add(GenerateTransactionFunc(from, preBlockHeight, preBlockHash));
        }
    }
}