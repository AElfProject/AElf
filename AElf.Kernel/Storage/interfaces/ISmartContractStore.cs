using System.Threading.Tasks;

namespace AElf.Kernel.Storage.Interfaces
{
    public interface ISmartContractStore
    {
        Task SetAsync(string key, object value);
        Task<T> GetAsync<T>(string key);
    }
}