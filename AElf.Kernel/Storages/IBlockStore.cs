using System.Threading.Tasks;
using AElf.Kernel.Extensions;

namespace AElf.Kernel.Storages
{
    public interface IBlockStore
    {
        Task Insert(Block block);

        Task<Block> GetAsync(Hash blockHash);
    }
    
    public class BlockStore : IBlockStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;
        
        public BlockStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }

        public async Task Insert(Block block)
        {
            await _keyValueDatabase.SetAsync(block.GetHash(), block);
        }

        public async Task<Block> GetAsync(Hash blockHash)
        {
            return (Block) await _keyValueDatabase.GetAsync(blockHash);
        }
    }
    
}