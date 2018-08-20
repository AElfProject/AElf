using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.SmartContract;

namespace AElf.ChainController
{
    public interface IExecutingService
    {
        Task<List<TransactionTrace>> ExecuteAsync(List<ITransaction> transactions, Hash chainId, CancellationToken cancellationToken);
    }
}