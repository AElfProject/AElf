using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Manager.Interfaces;
using AElf.Kernel.Storage;

namespace AElf.Kernel.Manager.Managers
{
    public class StateManager : IStateManager
    {
        private readonly IKeyValueStore _stateStore;

        public StateManager(StateStore stateStore)
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

        public async Task<bool> PipelineSetAsync(Dictionary<StatePath, byte[]> pipelineSet)
        {
            var dict = pipelineSet.ToDictionary(kv => GetStringKey(kv.Key), kv => (object) kv.Value);
            return await _stateStore.PipelineSetAsync(dict);
        }

        private string GetStringKey(StatePath path)
        {
            return path.GetHash().ToHex();
        }
    }
}