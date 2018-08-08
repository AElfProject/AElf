using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface ICurrentHashStore
    {
        Task InsertOrUpdateAsync(Hash chainId, Hash currentHash);

        Task<Hash> GetAsync(Hash chainId);
    }
}