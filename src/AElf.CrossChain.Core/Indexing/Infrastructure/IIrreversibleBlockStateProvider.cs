using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.CrossChain.Indexing.Infrastructure
{
    public interface IIrreversibleBlockStateProvider
    {
        Task<Block> GetNotIndexedIrreversibleBlockByHeightAsync(long height);
        Task<LastIrreversibleBlockDto> GetLastIrreversibleBlockHashAndHeightAsync();
        Task<bool> ValidateIrreversibleBlockExistingAsync();
    }
}