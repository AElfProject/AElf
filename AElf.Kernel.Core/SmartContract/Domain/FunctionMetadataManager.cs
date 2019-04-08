using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;
using Microsoft.Extensions.Options;

namespace AElf.Kernel.SmartContract.Domain
{
    public class FunctionMetadataManager : IFunctionMetadataManager
    {
        private readonly IBlockchainStore<FunctionMetadata> _functionMetadataStore;
        private readonly IBlockchainStore<SerializedCallGraph> _callGraphStore;
        private readonly int _chainId;

        public FunctionMetadataManager(IBlockchainStore<FunctionMetadata> functionMetadataStore,
            IBlockchainStore<SerializedCallGraph> callGraphStore, IOptionsSnapshot<ChainOptions> options)
        {
            _functionMetadataStore = functionMetadataStore;
            _callGraphStore = callGraphStore;
            _chainId = options.Value.ChainId;
        }


        private string GetMetadataKey(string name)
        {
            return _chainId.ToStorageKey() + KernelConsts.StorageKeySeparator + name;
        }

        public async Task AddMetadataAsync(string name, FunctionMetadata metadata)
        {
            var key = GetMetadataKey(name);
            await _functionMetadataStore.SetAsync(key, metadata);
        }

        public async Task<FunctionMetadata> GetMetadataAsync(string name)
        {
            var key = GetMetadataKey(name);
            return await _functionMetadataStore.GetAsync(key);
        }

        public async Task RemoveMetadataAsync(string name)
        {
            var key = GetMetadataKey(name);
            await _functionMetadataStore.RemoveAsync(key);
        }

        public async Task AddCallGraphAsync(SerializedCallGraph callGraph)
        {
            await _callGraphStore.SetAsync(_chainId.ToStorageKey(), callGraph);
        }

        public async Task<SerializedCallGraph> GetCallGraphAsync()
        {
            return await _callGraphStore.GetAsync(_chainId.ToStorageKey());
        }
    }
}