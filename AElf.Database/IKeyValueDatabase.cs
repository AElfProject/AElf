using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Database
{
    public interface IKeyValueDatabase
    {
        Task<byte[]> GetAsync(string key,Type type);
        Task SetAsync(string key, byte[] bytes);
        Task<bool> PipelineSetAsync(IEnumerable<KeyValuePair<string, byte[]>> queue);
        bool IsConnected();
    }
}