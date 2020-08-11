using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Miner.Application;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Contracts.MultiToken
{
    public class TestTokenBalanceTransactionGenerator : ISystemTransactionGenerator, ISingletonDependency
    {
        public Func<Address, long, Hash, Transaction> GenerateTransactionFunc { get; set; }

        public Task<List<Transaction>> GenerateTransactionsAsync(Address @from, long preBlockHeight, Hash preBlockHash)
        {
            var res = new List<Transaction>();
            if (GenerateTransactionFunc == null)
            {
                return Task.FromResult(res);
            }

            res.Add(GenerateTransactionFunc(from, preBlockHeight, preBlockHash));
            return Task.FromResult(res);
        }
    }
}