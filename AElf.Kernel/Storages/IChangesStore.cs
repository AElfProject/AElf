using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IChangesStore
    {
        Task InsertAsync(Hash path, Change before);
    }
}