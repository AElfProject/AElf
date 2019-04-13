using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Kernel;
using AElf.SmartContract;

namespace AElf.Execution
{
    public interface IParallelTransactionExecutingService : IExecutingService
    {
        int TimeoutMilliSeconds { get; set; }
        Task<List<TransactionTrace>> ExecuteAsync(List<ITransaction> transactions, Hash chainId);
    }
}