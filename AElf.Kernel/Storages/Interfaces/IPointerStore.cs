using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IPointerStore
    {
        Task InsertAsync(Hash path, Hash pointer);
        Task<Hash> GetAsync(Hash path);
    }
}