using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Manager.Interfaces
{
    public interface IStateManager
    {
        Task SetAsync(StatePath path, byte[] value);

        Task<byte[]> GetAsync(StatePath path);

        Task<bool> PipelineSetAsync(Dictionary<StatePath, byte[]> pipelineSet);
    }
}