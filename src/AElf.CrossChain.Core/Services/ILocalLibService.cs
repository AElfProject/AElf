using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.CrossChain
{
    public interface ILocalLibService
    {
        Task<Block> GetIrreversibleBlockByHeightAsync(long height);
        Task<long> GetLibHeight();
    }
}