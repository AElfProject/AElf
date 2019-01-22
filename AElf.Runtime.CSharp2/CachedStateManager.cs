using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.SmartContract;

namespace AElf.Runtime.CSharp
{
    public class CachedStateManager : IStateManager
    {
        private readonly IStateManager _plainStateManager;
        public Dictionary<StatePath, StateCache> Cache { get; set; } = new Dictionary<StatePath, StateCache>();

        public CachedStateManager(IStateManager plainStateManager)
        {
            _plainStateManager = plainStateManager;
        }

        public async Task SetAsync(StatePath path, byte[] value)
        {
            await _plainStateManager.SetAsync(path, value);
            Cache[path] = new StateCache(value);
        }

        public async Task<byte[]> GetAsync(StatePath path)
        {
            if (!Cache.TryGetValue(path, out var value))
            {
                var data = await _plainStateManager.GetAsync(path);
                value = new StateCache(data);
                Cache[path] = value;
            }

            return value.CurrentValue;
        }

        public async Task PipelineSetAsync(Dictionary<StatePath, byte[]> pipelineSet)
        {
            await _plainStateManager.PipelineSetAsync(pipelineSet);
            foreach (var kv in pipelineSet)
            {
                Cache[kv.Key] = new StateCache(kv.Value);
            }
        }
    }
}