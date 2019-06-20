using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Types;

namespace AElf.Kernel
{
    public class TestBlockExecutingService : IBlockExecutingService
    {
        public Task<Block> ExecuteBlockAsync(BlockHeader blockHeader,
            IEnumerable<Transaction> nonCancellableTransactions)
        {
            var block = new Block()
            {
                Header = blockHeader,
                Body = new BlockBody()
                {
                    BlockHeader = blockHeader.GetHash(),
                }
            };
            block.Body.Transactions.AddRange(nonCancellableTransactions.Select(p => p.GetHash()));

            return Task.FromResult(block);
        }

        public Task<Block> ExecuteBlockAsync(BlockHeader blockHeader,
            IEnumerable<Transaction> nonCancellableTransactions,
            IEnumerable<Transaction> cancellableTransactions, CancellationToken cancellationToken)
        {
            var block = new Block()
            {
                Header = blockHeader,
                Body = new BlockBody()
                {
                    BlockHeader = blockHeader.GetHash(),
                }
            };
            block.Body.Transactions.AddRange(nonCancellableTransactions.Concat(cancellableTransactions)
                .Select(p => p.GetHash()));

            return Task.FromResult(block);
        }
    }
}