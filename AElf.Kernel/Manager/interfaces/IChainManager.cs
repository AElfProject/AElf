using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Manager.Interfaces
{
    public interface IChainManager
    {
        Task AddChainAsync(Hash chainId, Hash genesisBlockHash);
        Task UpdateCurrentBlockHashAsync(Hash chainId, Hash blockHash);
        Task<Hash> GetCurrentBlockHashAsync(Hash chainId);
        Task UpdateCurrentBlockHeightAsync(Hash chainId, ulong height);
        Task<ulong> GetCurrentBlockHeightAsync(Hash chainId);
    }
}