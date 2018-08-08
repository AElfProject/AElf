using System;
using System.Threading.Tasks;
using AElf.Database;

namespace AElf.Kernel.Storages
{
    public class BlockHeaderStore : IBlockHeaderStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;
        private static uint TypeIndex => (uint) Types.BlockHeader;

        public BlockHeaderStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }

        public async Task<BlockHeader> InsertAsync(BlockHeader header)
        {
            var key = header.GetHash().GetKeyString(TypeIndex);
            await _keyValueDatabase.SetAsync(key, header.Serialize());
            return header;
        }

        public async Task<BlockHeader> GetAsync(Hash blockHash)
        {
            var key = blockHash.GetKeyString(TypeIndex);
            var bytes = await _keyValueDatabase.GetAsync(key, typeof(BlockHeader));
            if (bytes == null)
            {
                return null;
            }
            return BlockHeader.Parser.ParseFrom(bytes);
        }
    }
}