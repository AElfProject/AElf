using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Types;

namespace AElf.OS.BlockSync.Application
{
    public interface IBlockSyncBlockService
    {
        Task AddBlockWithTransactionsAsync(Block block, IEnumerable<Transaction> transactions);
    }
}