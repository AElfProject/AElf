using System.Threading.Tasks;
using AElf.Database;
using Google.Protobuf;

namespace AElf.Kernel.Storages
{
    public class BlockHeaderStore : IBlockHeaderStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;
        
        public BlockHeaderStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }

        public async Task<BlockHeader> InsertAsync(BlockHeader header)
        {
            await _keyValueDatabase.SetAsync(header.GetHash().Value.ToBase64(), header.Serialize());
            return header;
        }

        public async Task<BlockHeader> GetAsync(Hash blockHash)
        {
            return BlockHeader.Parser.ParseFrom(await _keyValueDatabase.GetAsync(blockHash.Value.ToBase64(), typeof(BlockHeader)));
        }
    }
}