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
        
        public FunctionFunctionMetadataManager(IFunctionMetadataStore functionMetadataStore)
        {
            _functionMetadataStore = functionMetadataStore;
        }

        private string GetMetadataKey(Hash chainId, string key)
        {
            return DataPath.CalculatePointerForMetadata(chainId, key).DumpHex();
        }
        
        public async Task AddAsync(Hash chainId, string name, FunctionMetadata metadata)
        {
            var key = GetMetadataKey(chainId, name);
            await _functionMetadataStore.SetAsync(key, metadata);
        }

        public async Task<FunctionMetadata> GetAsync(Hash chainId, string name)
        {
            var key = GetMetadataKey(chainId, name);
            return await _functionMetadataStore.GetAsync<FunctionMetadata>(key);
        }
        
        public async Task RemoveAsync(Hash chainId, string name)
        {
            var key = GetMetadataKey(chainId, name);
            await _functionMetadataStore.RemoveAsync(key);
        }
    }
}