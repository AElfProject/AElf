using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Types;

namespace AElf.Kernel.Storages
{
    public interface IDataStore
    {
        Task SetDataAsync(Hash pointerHash, byte[] data);
        Task<byte[]> GetDataAsync(Hash pointerHash);
        Task<bool> PipelineSetDataAsync(IEnumerable<KeyValuePair<string, byte[]>> queue);
    }
}