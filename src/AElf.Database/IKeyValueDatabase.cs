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
        Task SetAllAsync(Dictionary<string, byte[]> cache);
        bool IsConnected();
    }
}