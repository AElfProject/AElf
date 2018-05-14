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

        public async Task InsertAsync(IBlockHeader block)
        {
            await _keyValueDatabase.SetAsync(block.GetHash().Value.ToBase64(), block.Serialize());
        }

        public async Task<IBlockHeader> GetAsync(Hash blockHash)
        {
            return BlockHeader.Parser.ParseFrom(await _keyValueDatabase.GetAsync(blockHash.Value.ToBase64(), typeof(BlockHeader)));
        }
    }
}