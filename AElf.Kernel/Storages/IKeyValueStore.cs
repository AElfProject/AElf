using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IKeyValueStore
    {
        Task<bool> SetAsync(string key, object value);
        Task<bool> PipelineSetAsync(Dictionary<string, object> pipelineSet);
        Task<T> GetAsync<T>(string key);
        Task<bool> RemoveAsync(string key);
    }
}