using System.Threading.Tasks;
using AElf.Kernel.Concurrency.Scheduling;
using AElf.Kernel.Types;

namespace AElf.Kernel.Miner
{
    public interface IBlockExecutor
    {
        Task<bool> ExecuteBlock(IBlock block);
        void Start(IGrouper grouper);
    }
}