using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IPointerCollection
    {
        Task UpdateAsync(Hash path, Hash pointer);
        Task<Hash> GetAsync(Hash path);
    }
}