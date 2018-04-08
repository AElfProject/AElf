using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public class BlockBodyStore : IBlockBodyStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;

        public BlockBodyStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }

        public async Task InsertAsync(Hash txsMerkleTreeRoot, BlockBody body)
        {
            await _keyValueDatabase.SetAsync(txsMerkleTreeRoot, body);
        }

        public async Task<BlockBody> GetAsync(Hash blockHash)
        {
            return (BlockBody) await _keyValueDatabase.GetAsync(blockHash, typeof(BlockBody));
        }
    }
}