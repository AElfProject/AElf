using System.Threading.Tasks;
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

        public async Task InsertAsync(Hash txsMerkleTreeRoot, BlockBody body)
        {
            await _keyValueDatabase.SetAsync(txsMerkleTreeRoot.Value.ToBase64(), body.ToByteArray());
        }

        public async Task<BlockBody> GetAsync(Hash blockHash)
        {
            var blockBody =  await _keyValueDatabase.GetAsync(blockHash.Value.ToBase64(), typeof(BlockBody));
            return BlockBody.Parser.ParseFrom(blockBody);
        }
    }
}