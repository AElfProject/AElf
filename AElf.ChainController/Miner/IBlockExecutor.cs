using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.ChainController
{
    public interface IBlockExecutor
    {
        Task<bool> ExecuteBlock(IBlock block);
        void Start();
    }
}