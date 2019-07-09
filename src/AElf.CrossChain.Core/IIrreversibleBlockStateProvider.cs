using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Types;

namespace AElf.CrossChain
{
    public interface IIrreversibleBlockStateProvider
    {
        Task<Block> GetIrreversibleBlockByHeightAsync(long height);
        Task<long> GetLastIrreversibleBlockHeightAsync();
        Task<Hash> GetLastIrreversibleBlockHashAsync();
        Task<LastIrreversibleBlockDto> GetLibHashAndHeightAsync();
        Task<bool> ValidateIrreversibleBlockExistsAsync();
    }
}