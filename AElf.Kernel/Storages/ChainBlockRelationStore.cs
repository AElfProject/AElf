using System;
using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    /// <summary>
    /// Simply use a dictionary to store and get the relations.
    /// </summary>
    public class ChainBlockRelationStore : IChainBlockRelationStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;

        public ChainBlockRelationStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }
        
        public async Task InsertAsync(Chain chain, Block block)
        {
            throw new NotImplementedException();
            //await _keyValueDatabase.SetAsync(chain.NextBlockRelationHash, block.GetHash());
        }

        public async Task<Hash> GetAsync(Hash chainId, long height)
        {
            throw new NotImplementedException();
            //var hash = new Hash(chainId.CalculateHashWith(height));
            //return (Hash) await _keyValueDatabase.GetAsync(hash);
        }
    }
}