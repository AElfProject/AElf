using System.Threading.Tasks;
using AElf.Database;
using Google.Protobuf;

namespace AElf.Kernel.Storages
{
    public class BlockBodyStore : IBlockBodyStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;

        public BlockBodyStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }

        public async Task InsertAsync(Hash txsMerkleTreeRoot, IBlockBody body)
        {
            await _keyValueDatabase.SetAsync(txsMerkleTreeRoot.Value.ToBase64(), body.Serialize());
        }

        public async Task<BlockBody> GetAsync(Hash blockHash)
        {
            var blockBody =  await _keyValueDatabase.GetAsync(blockHash.Value.ToBase64(), typeof(BlockBody));
            return BlockBody.Parser.ParseFrom(blockBody);
        }
    }
}