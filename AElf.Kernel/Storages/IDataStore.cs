using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf;

namespace AElf.Kernel.Storages
{
    public interface IDataStore
    {
        Task SetDataAsync<T>(Hash pointerHash, byte[] data) where T : IMessage;
        Task<byte[]> GetDataAsync<T>(Hash pointerHash) where T : IMessage;
        Task<bool> PipelineSetDataAsync<T>(Dictionary<Hash, byte[]> pipelineSet) where T : IMessage;
    }
}