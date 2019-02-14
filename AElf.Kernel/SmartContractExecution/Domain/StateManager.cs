using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Storages;

namespace AElf.Kernel.Managers
{
    //TODO: remove
    public class StateManager : IStateManager
    {
        private readonly IStateStore _stateStore;

        public StateManager(IStateStore stateStore)
        {
            _stateStore = stateStore;
        }

        public async Task SetAsync(StatePath path, byte[] value)
        {
            await _stateStore.SetAsync(GetStringKey(path), value);
        }

        public async Task<byte[]> GetAsync(StatePath path)
        {
            var result = await _stateStore.GetAsync<byte[]>(GetStringKey(path));
            return result;
        }

        public async Task PipelineSetAsync(Dictionary<StatePath, byte[]> pipelineSet)
        {
            var dict = pipelineSet.ToDictionary(kv => GetStringKey(kv.Key), kv => (object) kv.Value);
            await _stateStore.PipelineSetAsync(dict);
        }

        private string GetStringKey(StatePath path)
        {
            return path.GetHash().ToHex();
        }
    }
}