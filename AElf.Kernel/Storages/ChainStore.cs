using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public class ChainStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;

        public ChainStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }
    
        public async Task<Chain> GetAsync(Hash id)
        {
            return (Chain) await _keyValueDatabase.GetAsync(id,typeof(Chain));
        }

        public async Task<Chain> UpdateAsync(Chain chain)
        {
            // TODO: So slow, need to find a way to speed up.
            await _keyValueDatabase.SetAsync(chain.Id, chain);
            return chain;
        }

        public async Task<Chain> InsertAsync(Chain chain)
        {
            await _keyValueDatabase.SetAsync(chain.Id, chain);
            return chain;
        }
    }
}