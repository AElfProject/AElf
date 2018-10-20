using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IStateStore
    {
        Task SetAsync(StatePath path, byte[] value);

        Task<byte[]> GetAsync(StatePath path);

        Task<bool> PipelineSetDataAsync(Dictionary<StatePath, byte[]> pipelineSet);
    }
}