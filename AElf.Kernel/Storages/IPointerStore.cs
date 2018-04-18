using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IPointerStore
    {
        Task UpdateAsync(Hash path, Hash pointer);
        Task<Hash> GetAsync(Hash path);
    }
}