using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IGenesisHashStore
    {
        Task InsertAsync(Hash chainId, Hash genesisHash);

        Task<Hash> GetAsync(Hash chainId);
    }
}