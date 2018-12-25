using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Managers
{
    public interface IPointerManager
    {
        Task AddPointerAsync(Hash pointer, Hash value);
        Task UpdatePointerAsync(Hash pointer, Hash value);
        Task<Hash> GetPointerAsync(Hash pointer);
        Task RemovePointer(Hash pointer);
    }
}