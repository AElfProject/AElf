using System.Threading.Tasks;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract.Infrastructure;

namespace AElf.Kernel.SmartContract.Domain
{
    public class FunctionFunctionMetadataManager : IFunctionMetadataManager
    {
        private readonly IFunctionMetadataStore _functionMetadataStore;
        private readonly ICallGraphStore _callGraphStore;
        
        public FunctionFunctionMetadataManager(IFunctionMetadataStore functionMetadataStore, ICallGraphStore callGraphStore)
        {
            _functionMetadataStore = functionMetadataStore;
            _callGraphStore = callGraphStore;
        }

        private string GetMetadataKey(int chainId, string name)
        {
            return chainId.ToStorageKey() + name;
        }
        
        public async Task AddMetadataAsync(int chainId, string name, FunctionMetadata metadata)
        {
            var key = GetMetadataKey(chainId, name);
            await _functionMetadataStore.SetAsync(key, metadata);
        }

        public async Task<FunctionMetadata> GetMetadataAsync(int chainId, string name)
        {
            var key = GetMetadataKey(chainId, name);
            return await _functionMetadataStore.GetAsync<FunctionMetadata>(key);
        }
        
        public async Task RemoveMetadataAsync(int chainId, string name)
        {
            var key = GetMetadataKey(chainId, name);
            await _functionMetadataStore.RemoveAsync(key);
        }

        public async Task AddCallGraphAsync(int chainId, SerializedCallGraph callGraph)
        {
            await _callGraphStore.SetAsync(chainId.ToStorageKey(), callGraph);
        }
        
        public async Task<SerializedCallGraph> GetCallGraphAsync(int chainId)
        {
            return await _callGraphStore.GetAsync<SerializedCallGraph>(chainId.ToStorageKey());
        }
    }
}