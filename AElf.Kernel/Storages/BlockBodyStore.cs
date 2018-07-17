using System;
using System.Threading.Tasks;
using AElf.Database;

namespace AElf.Kernel.Storages
{
    public class BlockBodyStore : IBlockBodyStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;

        public BlockBodyStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }

        public async Task InsertAsync(Hash bodyHash, IBlockBody body)
        {
            await _keyValueDatabase.SetAsync(bodyHash.ToHex(), body.Serialize());
        }

        public async Task<BlockBody> GetAsync(Hash bodyHash)
        {
            try
            {
                var blockBody =  await _keyValueDatabase.GetAsync(bodyHash.ToHex(), typeof(BlockBody));
                return BlockBody.Parser.ParseFrom(blockBody);
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}