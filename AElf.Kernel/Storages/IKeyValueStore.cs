using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IKeyValueStore
    {
        Task SetAsync(string key, object value);
        Task PipelineSetAsync(Dictionary<string, object> pipelineSet);
        Task<T> GetAsync<T>(string key);
        Task RemoveAsync(string key);
    }
    
    public interface IKeyValueStore<T>
    {
        Task SetAsync(string key, T value);
        Task PipelineSetAsync(Dictionary<string, T> pipelineSet);
        Task<T> GetAsync(string key);
        Task RemoveAsync(string key);
    }

    /// <summary>
    /// key value will not changed. can be set and remove
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ICachedReadOnlyKeyValueStore<T> : IKeyValueStore<T>
    {
        
    }
}