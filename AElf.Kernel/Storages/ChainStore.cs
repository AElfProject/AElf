using System.Threading.Tasks;
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
    
        public async Task<Chain> GetAsync(Hash id)
        {
            return Chain.Parser.ParseFrom(await _keyValueDatabase.GetAsync(id.Value.ToBase64(), typeof(Chain)));
        }

        public async Task<Chain> UpdateAsync(Chain chain)
        {
            var bytes = chain.ToByteArray();
            await _keyValueDatabase.SetAsync(chain.Id.Value.ToBase64(), bytes);
            return chain;
        }

        public async Task<Chain> InsertAsync(Chain chain)
        {
            await _keyValueDatabase.SetAsync(chain.Id.Value.ToBase64(), chain.ToByteArray());
            return chain;
        }
    }
}