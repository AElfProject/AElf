using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Database
{
    public interface IKeyValueDatabase
    {
        Task<byte[]> GetAsync(string database, string key);
        Task SetAsync(string database, string key, byte[] bytes);
        Task RemoveAsync(string database, string key);
        Task<bool> PipelineSetAsync(string database, Dictionary<string, byte[]> cache);
        bool IsConnected(string database = "");
    }
}