using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common.Serializers;
using AElf.Database;
using AElf.Kernel.Exceptions;
using AElf.Kernel.Storage.Interfaces;

namespace AElf.Kernel.Storage.Storages
{
    public abstract class KeyValueStoreBase : IKeyValueStoreBase
    {
        protected readonly IKeyValueDatabase KeyValueDatabase;
        protected readonly IByteSerializer ByteSerializer;
        
        protected string DataPrefix { get; set; }

        protected KeyValueStoreBase(IKeyValueDatabase keyValueDatabase, IByteSerializer byteSerializer, string dataPrefix)
        {
            KeyValueDatabase = keyValueDatabase;
            ByteSerializer = byteSerializer;

            DataPrefix = dataPrefix;
        }

        public virtual async Task SetAsync(string key, object value)
        {
            CheckKey(key);
            CheckValue(value);

            var databaseKey = GetDatabaseKey(key);
            await KeyValueDatabase.SetAsync(DataPrefix, databaseKey, ByteSerializer.Serialize(value));
        }

        public virtual async Task<bool> PipelineSetAsync(Dictionary<string, object> pipelineSet)
        {
            var value = new Dictionary<string, byte[]>();
            foreach (var set in pipelineSet)
            {
                value.Add(GetDatabaseKey(set.Key), ByteSerializer.Serialize(set.Value));
            }

            return await KeyValueDatabase.PipelineSetAsync(DataPrefix, value);
        }

        public virtual async Task<T> GetAsync<T>(string key)
        {
            CheckKey(key);

            var databaseKey = GetDatabaseKey(key);
            var result = await KeyValueDatabase.GetAsync(DataPrefix, databaseKey);

            return result == null ? default(T) : ByteSerializer.Deserialize<T>(result);
        }

        public virtual async Task RemoveAsync(string key)
        {
            CheckKey(key);

            var databaseKey = GetDatabaseKey(key);
            await KeyValueDatabase.RemoveAsync(DataPrefix, databaseKey);
        }

        public string GetDatabaseKey(string key)
        {
            return $"{DataPrefix}{key}";
        }

        protected virtual void CheckKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null.");
            }
        }

        protected virtual void CheckValue(object value)
        {
            if (value == null)
            {
                throw new ArgumentException("Cannot insert null value.");
            }
        }

        protected virtual void CheckReturnValue(object result)
        {
            if (result == null)
            {
                throw new DataNotFoundException("Value not exist.");
            }
        }
    }
}