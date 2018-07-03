using System.Threading.Tasks;
using AElf.Common.ByteArrayHelpers;
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
            var chainBytes = await _keyValueDatabase.GetAsync(id.Value.ToByteArray().ToHex(), typeof(Chain));
            return chainBytes == null ? null : Chain.Parser.ParseFrom(chainBytes);
        }

        public async Task<IChain> UpdateAsync(IChain chain)
        {
            var bytes = chain.Serialize();
            await _keyValueDatabase.SetAsync(chain.Id.Value.ToByteArray().ToHex(), bytes);
            return chain;
        }

        public async Task<IChain> InsertAsync(IChain chain)
        {
            await _keyValueDatabase.SetAsync(chain.Id.Value.ToByteArray().ToHex(), chain.Serialize());
            return chain;
        }
    }
}