using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IChangesStore
    {
        Task Insert(IHash<IPath> path, IHash<IPath> before, IHash<IPath> after);
    }
}