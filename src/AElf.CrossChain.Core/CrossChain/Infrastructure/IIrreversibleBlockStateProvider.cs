using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.CrossChain
{
    public interface IIrreversibleBlockStateProvider
    {
        Task<Block> GetNotIndexedIrreversibleBlockByHeightAsync(long height);
        Task<LastIrreversibleBlockDto> GetLastIrreversibleBlockHashAndHeightAsync();
        Task<bool> ValidateIrreversibleBlockExistingAsync();
    }
}