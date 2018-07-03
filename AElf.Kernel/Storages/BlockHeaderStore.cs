using System.Threading.Tasks;
using AElf.Common.ByteArrayHelpers;
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
            await _keyValueDatabase.SetAsync(header.GetHash().ToHex(), header.Serialize());
            return header;
        }

        public async Task<BlockHeader> GetAsync(Hash blockHash)
        {
            return BlockHeader.Parser.ParseFrom(await _keyValueDatabase.GetAsync(blockHash.ToHex(), typeof(BlockHeader)));
        }
    }
}