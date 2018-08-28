using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf;

namespace AElf.Kernel.Storages
{
    public interface IDataStore
    {
        Task InsertAsync<T>(Hash pointerHash, T obj) where T : IMessage;
        Task<T> GetAsync<T>(Hash pointerHash) where T : IMessage, new();

        Task<bool> PipelineSetDataAsync(Dictionary<Hash, byte[]> pipelineSet);
        Task RemoveAsync<T>(Hash txId) where T : IMessage;
    }
}