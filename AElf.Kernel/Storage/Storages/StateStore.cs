using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;
using AElf.Kernel.Storage.Interfaces;

namespace AElf.Kernel.Storage.Storages
{
    public class StateStore : KeyValueStoreBase, IStateStore
    {

        public StateStore(IKeyValueDatabase keyValueDatabase, IByteSerializer byteSerializer)
            : base(keyValueDatabase, byteSerializer, GlobalConfig.StatePrefix)
        {
        }

        public override async Task SetAsync(string key, object value)
        {
            CheckKey(key);
            CheckValue(value);

            var databaseKey = GetDatabaseKey(key);
            await KeyValueDatabase.SetAsync(DataPrefix, databaseKey, (byte[]) value);
        }

        public override async Task<bool> PipelineSetAsync(Dictionary<string, object> pipelineSet)
        {
            var dict = pipelineSet.ToDictionary(kv => GetDatabaseKey(kv.Key), kv => (byte[])kv.Value);
            return await KeyValueDatabase.PipelineSetAsync(DataPrefix, dict);
        }

        public override async Task<T> GetAsync<T>(string key)
        {
            CheckKey(key);

            var databaseKey = GetDatabaseKey(key);
            var result = await KeyValueDatabase.GetAsync(DataPrefix, databaseKey);

            return (T) Convert.ChangeType(result, typeof(T));
        }
    }
}