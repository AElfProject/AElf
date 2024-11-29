using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Miner.Application;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus.AEDPoS.Application;

public class SyncBlsPubkeyTransactionGenerator : ISystemTransactionGenerator, ISingletonDependency
{
    public Task<List<Transaction>> GenerateTransactionsAsync(Address from, long preBlockHeight, Hash preBlockHash)
    {
        throw new System.NotImplementedException();
    }
}