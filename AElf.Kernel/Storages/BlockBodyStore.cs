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
            var key = bodyHash.GetKeyString(TypeName.TnBlockBody);
            await _keyValueDatabase.SetAsync(key, body.Serialize());
        }

        public async Task<BlockBody> GetAsync(Hash bodyHash)
        {
            try
            {
                var key = bodyHash.GetKeyString(TypeName.TnBlockBody);
                var blockBody =  await _keyValueDatabase.GetAsync(key, typeof(BlockBody));
                return BlockBody.Parser.ParseFrom(blockBody);
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}