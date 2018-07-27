using System;
using System.Threading.Tasks;
using AElf.Database;

namespace AElf.Kernel.Storages
{
    public class BlockBodyStore : IBlockBodyStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;
        private static uint TypeIndex => (uint) Types.BlockBody;

        public BlockBodyStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }

        public async Task InsertAsync(Hash bodyHash, IBlockBody body)
        {
            var key = bodyHash.GetKeyString(TypeIndex);
            await _keyValueDatabase.SetAsync(key, body.Serialize());
        }

        public async Task<BlockBody> GetAsync(Hash bodyHash)
        {
            var key = bodyHash.GetKeyString(TypeIndex);
            var blockBody =  await _keyValueDatabase.GetAsync(key);
            return BlockBody.Parser.ParseFrom(blockBody);
        }
    }
}