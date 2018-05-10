using System.Threading.Tasks;

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
            return (Chain) await _keyValueDatabase.GetAsync(id, typeof(Chain));
        }

        public async Task<Chain> UpdateAsync(Chain chain)
        {
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