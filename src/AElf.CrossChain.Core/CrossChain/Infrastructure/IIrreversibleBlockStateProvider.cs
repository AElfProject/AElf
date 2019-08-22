using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.CrossChain
{
    public interface IIrreversibleBlockStateProvider
    {
        Task<Block> GetIrreversibleBlockByHeightAsync(long height);
        Task<LastIrreversibleBlockDto> GetLastIrreversibleBlockHashAndHeightAsync();
        Task<bool> ValidateIrreversibleBlockExistingAsync();
    }
}