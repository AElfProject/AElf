using System.Threading.Tasks;
using AElf.Kernel.Types;
using AElf.Common;

namespace AElf.Kernel.Managers
{
    public interface IChainManager
    {
        Task AddChainAsync(int chainId, Hash genesisBlockHash);
        Task UpdateCurrentBlockHashAsync(int chainId, Hash blockHash);
        Task<Hash> GetCurrentBlockHashAsync(int chainId);
        Task UpdateCurrentBlockHeightAsync(int chainId, ulong height);
        Task<ulong> GetCurrentBlockHeightAsync(int chainId);
        Task SetCanonical(int chainId, ulong height, Hash canonical);
        Task<Hash> GetCanonical(int chainId, ulong height);
        Task RemoveCanonical(int chainId, ulong height);
    }
}