using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain;
using AElf.Types;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public interface IBlockExecutingService
    {
        Task<BlockExecutedSet> ExecuteBlockAsync(BlockHeader blockHeader, List<Transaction> nonCancellableTransactions);

        Task<BlockExecutedSet> ExecuteBlockAsync(BlockHeader blockHeader, IEnumerable<Transaction> nonCancellableTransactions,
            IEnumerable<Transaction> cancellableTransactions, CancellationToken cancellationToken);
    }
}