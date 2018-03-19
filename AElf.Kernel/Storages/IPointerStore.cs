using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IPointerStore
    {
        Task Insert(Hash<Path> path, Hash<Path> pointer);
    }
}