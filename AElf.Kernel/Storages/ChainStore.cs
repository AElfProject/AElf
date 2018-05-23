using System.Threading.Tasks;
using AElf.Database;
using Google.Protobuf;
using ServiceStack;

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
            var chainBytes = await _keyValueDatabase.GetAsync(id.Value.ToBase64(), typeof(Chain));
            return chainBytes == null ? null : Chain.Parser.ParseFrom(chainBytes);
        }

        public async Task<IChain> UpdateAsync(IChain chain)
        {
            var bytes = chain.Serialize();
            await _keyValueDatabase.SetAsync(chain.Id.Value.ToBase64(), bytes);
            return chain;
        }

        public async Task<IChain> InsertAsync(IChain chain)
        {
            await _keyValueDatabase.SetAsync(chain.Id.Value.ToBase64(), chain.Serialize());
            return chain;
        }
    }
}