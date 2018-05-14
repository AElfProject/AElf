using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IChangesStore
    {
        Task InsertChangeAsync(Hash pathHash, Change change);
        Task<Change> GetChangeAsync(Hash pathHash);
        Task UpdatePointerAsync(Hash pathHash, Hash pointerHash);
        Task<Hash> GetPointerAsync(Hash pathHash);
    }
}