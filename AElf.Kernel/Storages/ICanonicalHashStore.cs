using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface ICanonicalHashStore
    {
        Task<Hash> InsertOrUpdateAsync(Hash heightHash, Hash blockHash);
        Task<Hash> GetAsync(Hash heightHash);
        Task RemoveAsync(Hash heightHash);
    }
}