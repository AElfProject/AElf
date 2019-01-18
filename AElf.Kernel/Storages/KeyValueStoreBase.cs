using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
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

    public abstract class KeyValueStoreBase<TKeyValueDbContext, T> : IKeyValueStore<T>
        where TKeyValueDbContext : KeyValueDbContext<TKeyValueDbContext>
    {
        protected readonly TKeyValueDbContext _keyValueDbContext;
        protected readonly IByteSerializer ByteSerializer;

        protected readonly IKeyValueCollection _collection;

        protected string DataPrefix { get; set; }

        protected KeyValueStoreBase(IByteSerializer byteSerializer, TKeyValueDbContext keyValueDbContext,
            string dataPrefix)
        {
            _keyValueDbContext = keyValueDbContext;
            ByteSerializer = byteSerializer;

            DataPrefix = dataPrefix;
            _keyValueDbContext = keyValueDbContext;
            _collection = keyValueDbContext.Collection(DataPrefix);
        }

        public async Task SetAsync(string key, T value)
        {
            await _collection.SetAsync(key, ByteSerializer.Serialize(value));
        }

        public async Task PipelineSetAsync(Dictionary<string, T> pipelineSet)
        {
            await _collection.PipelineSetAsync(
                pipelineSet.ToDictionary(k => k.Key, v => ByteSerializer.Serialize(v.Value)));
        }

        public virtual async Task<T> GetAsync(string key)
        {
            var result = await _collection.GetAsync(key);

            return result == null ? default(T) : ByteSerializer.Deserialize<T>(result);
        }

        public virtual async Task RemoveAsync(string key)
        {
            await _collection.RemoveAsync(key);
        }
    }


    public interface IStateStore<T> : IKeyValueStore<T>
    {
    }

    public class StateStore<T> : KeyValueStoreBase<StateKeyValueDbContext, T>, IStateStore<T>
    {
        public StateStore(IByteSerializer byteSerializer, StateKeyValueDbContext keyValueDbContext)
            : base(byteSerializer, keyValueDbContext, GlobalConfig.StatePrefix + typeof(T).Name)
        {
        }
    }
}