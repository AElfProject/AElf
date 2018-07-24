using System.Threading.Tasks;
using AElf.Database;
using AElf.Kernel.Types;

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
            var key = header.GetHash().GetKeyString(TypeName.TnBlockHeader);
            await _keyValueDatabase.SetAsync(key, header.Serialize());
            return header;
        }

        public async Task<BlockHeader> GetAsync(Hash blockHash)
        {
            var key = blockHash.GetKeyString(TypeName.TnBlockHeader);
            return BlockHeader.Parser.ParseFrom(await _keyValueDatabase.GetAsync(key, typeof(BlockHeader)));
        }
    }
}