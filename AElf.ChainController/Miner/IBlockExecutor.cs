using System.Threading.Tasks;
using AElf.Kernel;
using AElf.ChainController.Execution;

namespace AElf.ChainController
{
    public interface IBlockExecutor
    {
        Task<bool> ExecuteBlock(IBlock block);
        void Start();
    }
}