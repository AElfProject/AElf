using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.SmartContract;
using AElf.Kernel.Storages;

namespace AElf.Kernel.Managers
{
    public class FunctionFunctionMetadataManager : IFunctionMetadataManager
    {
        private readonly IKeyValueStore _functionMetadataStore;
        private readonly IKeyValueStore _callGraphStore;
        
        public FunctionFunctionMetadataManager(FunctionMetadataStore functionMetadataStore, CallGraphStore callGraphStore)
        {
            _functionMetadataStore = functionMetadataStore;
            _callGraphStore = callGraphStore;
        }

        private string GetMetadataKey(Hash chainId, string name)
        {
            return chainId.ToHex() + name;
        }
        
        public async Task AddMetadataAsync(Hash chainId, string name, FunctionMetadata metadata)
        {
            var key = GetMetadataKey(chainId, name);
            await _functionMetadataStore.SetAsync(key, metadata);
        }

        public async Task<FunctionMetadata> GetMetadataAsync(Hash chainId, string name)
        {
            var key = GetMetadataKey(chainId, name);
            return await _functionMetadataStore.GetAsync<FunctionMetadata>(key);
        }
        
        public async Task RemoveMetadataAsync(Hash chainId, string name)
        {
            var key = GetMetadataKey(chainId, name);
            await _functionMetadataStore.RemoveAsync(key);
        }

        public async Task AddCallGraphAsync(Hash chainId, SerializedCallGraph callGraph)
        {
            await _callGraphStore.SetAsync(chainId.ToHex(), callGraph);
        }
        
        public async Task<SerializedCallGraph> GetCallGraphAsync(Hash chainId)
        {
            return await _callGraphStore.GetAsync<SerializedCallGraph>(chainId.ToHex());
        }
    }
}