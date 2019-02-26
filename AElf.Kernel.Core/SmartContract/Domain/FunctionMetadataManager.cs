using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;

namespace AElf.Kernel.SmartContract.Domain
{
    public class FunctionMetadataManager : IFunctionMetadataManager
    {
        private readonly IBlockchainStore<FunctionMetadata> _functionMetadataStore;
        private readonly IBlockchainStore<SerializedCallGraph> _callGraphStore;

        public FunctionMetadataManager(IBlockchainStore<FunctionMetadata> functionMetadataStore,
            IBlockchainStore<SerializedCallGraph> callGraphStore)
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
            return await _functionMetadataStore.GetAsync(key);
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
            return await _callGraphStore.GetAsync(chainId.ToStorageKey());
        }
    }
}