using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common.Serializers;
using AElf.Database;
using AElf.Kernel.Exceptions;

namespace AElf.Kernel.Storages
{
    public abstract class KeyValueStoreBase<TKeyValueDbContext> : IKeyValueStore
        where TKeyValueDbContext : KeyValueDbContext<TKeyValueDbContext>
    {
        protected readonly TKeyValueDbContext _keyValueDbContext;
        protected readonly IByteSerializer ByteSerializer;

        protected readonly IKeyValueCollection _collection;

        protected string DataPrefix { get; set; }

        protected KeyValueStoreBase(IByteSerializer byteSerializer, TKeyValueDbContext keyValueDbContext,
            string dataPrefix)
        {
            ByteSerializer = byteSerializer;
            DataPrefix = dataPrefix;
            _keyValueDbContext = keyValueDbContext;
            _collection = keyValueDbContext.Collection(DataPrefix);
        }

        public virtual async Task SetAsync(string key, object value)
        {
            await _collection.SetAsync(key, ByteSerializer.Serialize(value));
        }

        public virtual async Task PipelineSetAsync(Dictionary<string, object> pipelineSet)
        {
            await _collection.PipelineSetAsync(
                pipelineSet.ToDictionary(k => k.Key, v => ByteSerializer.Serialize(v.Value)));
        }

        public virtual async Task<T> GetAsync<T>(string key)
        {
            var result = await _collection.GetAsync(key);

            return result == null ? default(T) : ByteSerializer.Deserialize<T>(result);
        }

        public virtual async Task RemoveAsync(string key)
        {
            await _collection.RemoveAsync(key);
        }
    }
}