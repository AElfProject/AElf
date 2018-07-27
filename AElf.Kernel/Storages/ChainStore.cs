using System;
using System.Threading.Tasks;
using AElf.Database;
using AElf.Kernel.Types;

namespace AElf.Kernel.Storages
{
    public class ChainStore: IChainStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;
        private static uint TypeIndex => (uint) Types.Chain;

        public ChainStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }
    
        public async Task<IChain> GetAsync(Hash id)
        {
            var key = id.GetKeyString(TypeIndex);
            var chainBytes = await _keyValueDatabase.GetAsync(key);
            return chainBytes == null ? null : Chain.Parser.ParseFrom(chainBytes);
        }

        public async Task<IChain> UpdateAsync(IChain chain)
        {
            var key = chain.Id.GetKeyString(TypeIndex);
            var bytes = chain.Serialize();
            await _keyValueDatabase.SetAsync(key, bytes);
            return chain;
        }

        public async Task<IChain> InsertAsync(IChain chain)
        {
            var key = chain.Id.GetKeyString(TypeIndex);
            await _keyValueDatabase.SetAsync(key, chain.Serialize());
            return chain;
        }
    }
}