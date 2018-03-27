using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IKeyValueDatabase
    {
        Task<object> GetAsync(Hash key);
        Task SetAsync(object bytes);
    }
}