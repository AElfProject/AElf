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

        public async Task InsertAsync(Hash txsMerkleTreeRoot, IBlockBody body)
        {
            await _keyValueDatabase.SetAsync(txsMerkleTreeRoot, body.Serialize());
        }

        public async Task<IBlockBody> GetAsync(Hash blockHash)
        {
            var blockBody =  await _keyValueDatabase.GetAsync(blockHash, typeof(BlockBody));
            return BlockBody.Parser.ParseFrom(blockBody);
        }
    }
}