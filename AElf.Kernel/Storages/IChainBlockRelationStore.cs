using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;

namespace AElf.Kernel.Storages
{
    public interface IChainBlockRelationStore
    {
        Task InsertAsync(Chain chain, Block block);

        Task<Hash> GetAsync(Hash chainHash, long height);
    }
    
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
            await _keyValueDatabase.SetAsync(chain.NextBlockRelationHash, block.GetHash());
        }

        public async Task<Hash> GetAsync(Hash chainId, long height)
        {
            var hash = new Hash(chainId.CalculateHashWith(height));
            return (Hash) await _keyValueDatabase.GetAsync(hash);
        }
    }
}