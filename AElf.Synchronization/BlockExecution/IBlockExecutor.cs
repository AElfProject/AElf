using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.Synchronization.BlockExecution
{
    public interface IBlockExecutor
    {
        Task<BlockExecutionResult> ExecuteBlock(IBlock block);
    }
}