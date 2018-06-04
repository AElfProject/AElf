using System.Threading.Tasks;
using AElf.Database;

namespace AElf.Kernel.Storages
{
    public interface IDataStore
    {
        Task SetDataAsync(Hash pointerHash, Data data);
        Task<Data> GetDataAsync(Hash pointerHash);
    }
}