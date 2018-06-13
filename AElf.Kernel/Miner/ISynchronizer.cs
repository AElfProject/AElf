using System.Threading.Tasks;

namespace AElf.Kernel.Miner
{
    public interface ISynchronizer
    {
        Task<bool> ExecuteBlock(IBlock block);
    }
}