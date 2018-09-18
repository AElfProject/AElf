using System.Threading.Tasks;
using AElf.Kernel.Types;

namespace AElf.Kernel.Managers
{
    public interface IChainManagerBasic
    {
        Task AddChainAsync(Hash chainId, Hash genesisBlockHash);
        Task<Hash> GetGenesisBlockHashAsync(Hash chainId);
        Task UpdateCurrentBlockHashAsync(Hash chainId, Hash blockHash);
        Task<Hash> GetCurrentBlockHashAsync(Hash chainId);
        Task UpdateCurrentBlockHeightAsync(Hash chainId, ulong height);
        Task<ulong> GetCurrentBlockHeightAsync(Hash chainId);
        Task AddSideChainId(Hash chainId);
        Task<SideChainIdList> GetSideChainIdList();
    }
}