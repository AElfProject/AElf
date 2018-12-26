using System.Threading.Tasks;
using AElf.Kernel.Types;
using AElf.Common;

namespace AElf.Kernel.Managers
{
    public interface IChainManager
    {
        Task AddChainAsync(Hash chainId, Hash genesisBlockHash);
        Task UpdateCurrentBlockHashAsync(Hash chainId, Hash blockHash);
        Task<Hash> GetCurrentBlockHashAsync(Hash chainId);
        Task UpdateCurrentBlockHeightAsync(Hash chainId, ulong height);
        Task<ulong> GetCurrentBlockHeightAsync(Hash chainId);
        Task SetCanonical(Hash chainId, ulong height, Hash canonical);
        Task<Hash> GetCanonical(Hash chainId, ulong height);
        Task RemoveCanonical(Hash chainId, ulong height);
    }
}