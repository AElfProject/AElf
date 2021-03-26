using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Database
{
    public interface IKeyValueDatabase<TKeyValueDbContext>
        where TKeyValueDbContext: KeyValueDbContext<TKeyValueDbContext>
    {
        Task<byte[]> GetAsync(string key);
        Task SetAsync(string key, byte[] bytes);
        Task RemoveAsync(string key);
        Task<List<byte[]>> GetAllAsync(IList<string> keys);
        Task SetAllAsync(IDictionary<string, byte[]> values);
        Task RemoveAllAsync(IList<string> keys);
        Task<bool> IsExistsAsync(string key);
        bool IsConnected();
    }
}