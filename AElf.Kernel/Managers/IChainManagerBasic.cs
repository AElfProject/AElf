using System.Threading.Tasks;
using AElf.Kernel.Types;

namespace AElf.Kernel.Managers
{
    public interface IChainManagerBasic
    {
        Task AddChainAsync(Hash chainId, Hash genesisBlockHash);
        Task<Hash> GetGenesisBlockHashAsync(Hash chainId);
        Task<Hash> GetCurrentBlockHashAsync(Hash chainId);
    }
}