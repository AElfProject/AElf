using System.Threading.Tasks;
using AElf.Database;
using AElf.Kernel.Types;

namespace AElf.Kernel.Storages
{
    public class ChainStore: IChainStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;

        public ChainStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }
    
        public async Task<IChain> GetAsync(Hash id)
        {
            var key = id.GetKeyString(TypeName.TnChain);
            var chainBytes = await _keyValueDatabase.GetAsync(key, typeof(Chain));
            return chainBytes == null ? null : Chain.Parser.ParseFrom(chainBytes);
        }

        public async Task<IChain> UpdateAsync(IChain chain)
        {
            var key = chain.Id.GetKeyString(TypeName.TnChain);
            var bytes = chain.Serialize();
            await _keyValueDatabase.SetAsync(key, bytes);
            return chain;
        }

        public async Task<IChain> InsertAsync(IChain chain)
        {
            var key = chain.Id.GetKeyString(TypeName.TnChain);
            await _keyValueDatabase.SetAsync(key, chain.Serialize());
            return chain;
        }
    }
}