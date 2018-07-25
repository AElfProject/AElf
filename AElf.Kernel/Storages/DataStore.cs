using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Database;
using System.Linq;

namespace AElf.Kernel.Storages
{
    public class DataStore : IDataStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;

        public DataStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }
               
        public async Task SetDataAsync(Hash pointerHash, TypeName typeName, byte[] data)
        {
            var key = pointerHash.GetKeyString(typeName);
            await _keyValueDatabase.SetAsync(key, data);
        }

        public async Task<byte[]> GetDataAsync(Hash pointerHash, TypeName typeName)
        {
            if (pointerHash == null)
            {
                return null;
            }
            var key = pointerHash.GetKeyString(typeName);
            return await _keyValueDatabase.GetAsync(key, typeof(byte[]));
        }

        public async Task<bool> PipelineSetDataAsync(Dictionary<Hash, byte[]> pipelineSet, TypeName typeName)
        {

            return await _keyValueDatabase.PipelineSetAsync(
                pipelineSet.ToDictionary(kv => kv.Key.GetKeyString(typeName), kv => kv.Value));
        }
    }
}