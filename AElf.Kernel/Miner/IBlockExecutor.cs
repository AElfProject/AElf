using System.Threading.Tasks;

namespace AElf.Kernel.Miner
{
    public interface IBlockExecutor
    {
        Task<bool> ExecuteBlock(IBlock block);
    }
}