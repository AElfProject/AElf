using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Services.Execution;

namespace AElf.Services.Miner
{
    public interface IBlockExecutor
    {
        Task<bool> ExecuteBlock(IBlock block);
        void Start(IGrouper grouper);
    }
}