using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Manager.Interfaces;
using AElf.Kernel.SmartContract;
using AElf.Kernel.Storage.Interfaces;

namespace AElf.Kernel.Manager.Managers
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

        private string GetMetadataKey(Hash chainId, string name)
        {
            return chainId.DumpHex() + name;
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
            await _callGraphStore.SetAsync(chainId.DumpHex(), callGraph);
        }
        
        public async Task<SerializedCallGraph> GetCallGraphAsync(Hash chainId)
        {
            return await _callGraphStore.GetAsync<SerializedCallGraph>(chainId.DumpHex());
        }
    }
}