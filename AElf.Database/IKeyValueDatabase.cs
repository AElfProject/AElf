using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Database
{
    public interface IKeyValueDatabase
    {
        Task<byte[]> GetAsync(string key,Type type);
        Task SetAsync(string key, byte[] bytes);
        Task RemoveAsync(string key);
        Task<bool> PipelineSetAsync(Dictionary<string, byte[]> cache);
        bool IsConnected();
    }
}