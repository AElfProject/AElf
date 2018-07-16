using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IDataStore
    {
        Task SetDataAsync(Hash pointerHash, byte[] data);
        Task<byte[]> GetDataAsync(Hash pointerHash);
        Task<bool> PipelineSetDataAsync(Dictionary<Hash, byte[]> pipelineSet);
    }
}