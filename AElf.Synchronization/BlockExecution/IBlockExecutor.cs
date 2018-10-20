using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Kernel;

namespace AElf.Synchronization.BlockExecution
{
    public interface IBlockExecutor
    {
        Task<BlockExecutionResult> ExecuteBlock(IBlock block);
        void Init();
    }
}