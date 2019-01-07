using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Database
{
    public interface IKeyValueDatabase<TKeyValueDbContext>
        where TKeyValueDbContext: KeyValueDbContext<TKeyValueDbContext>
    {
        Task<byte[]> GetAsync(string key);
        Task<bool> SetAsync(string key, byte[] bytes);
        Task<bool> RemoveAsync(string key);
        Task<bool> PipelineSetAsync(Dictionary<string, byte[]> cache);
        bool IsConnected();
    }
}