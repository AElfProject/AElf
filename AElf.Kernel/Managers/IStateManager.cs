using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Managers
{
    public interface IStateManager
    {
        Task SetAsync(StatePath path, byte[] value);

        Task<byte[]> GetAsync(StatePath path);

        Task PipelineSetAsync(Dictionary<StatePath, byte[]> pipelineSet);
    }
}