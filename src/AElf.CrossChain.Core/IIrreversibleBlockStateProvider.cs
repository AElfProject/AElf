using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Types;

namespace AElf.CrossChain
{
    public interface IIrreversibleBlockStateProvider
    {
        Task<Block> GetIrreversibleBlockByHeightAsync(long height);
        Task<LastIrreversibleBlockDto> GetLastIrreversibleBlockHashAndHeightAsync();
        Task<bool> ValidateIrreversibleBlockExistingAsync();
    }
}