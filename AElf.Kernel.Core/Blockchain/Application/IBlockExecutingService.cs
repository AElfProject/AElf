using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Blockchain.Application
{
    public interface IBlockExecutingService
    {
        Task<Block> ExecuteBlockAsync(int chainId, BlockHeader blockHeader, IEnumerable<Transaction> nonCancellableTransactions);

        Task<Block> ExecuteBlockAsync(int chainId, BlockHeader blockHeader, IEnumerable<Transaction> nonCancellableTransactions,
            IEnumerable<Transaction> cancellableTransactions, CancellationToken cancellationToken);
    }
}