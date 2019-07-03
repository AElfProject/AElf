using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf;

namespace AElf.Kernel.Infrastructure
{
    public interface IKeyValueStore<T>
        where T: IMessage<T>
    {
        Task SetAsync(string key, T value);
        Task SetAllAsync(Dictionary<string, T> pipelineSet);
        Task<T> GetAsync(string key);
        Task RemoveAsync(string key);
    }
}