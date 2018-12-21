using System.Threading.Tasks;

namespace AElf.Kernel.Storage.Interfaces
{
    public interface ITransactionStore
    {
        Task SetAsync(string key, object value);
        Task<T> GetAsync<T>(string key);
        Task RemoveAsync(string key);
    }
}