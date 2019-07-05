using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Database;
using Google.Protobuf;

namespace AElf.Kernel.Infrastructure
{
    public interface IStoreKeyPrefixProvider<T>
        where T : IMessage<T>, new()
    {
        string GetStoreKeyPrefix();
    }

    public class StoreKeyPrefixProvider<T> : IStoreKeyPrefixProvider<T>
        where T : IMessage<T>, new()
    {
        private static readonly string _typeName = typeof(T).Name;

        public string GetStoreKeyPrefix()
        {
            return _typeName;
        }
    }

    public class FastStoreKeyPrefixProvider<T> : IStoreKeyPrefixProvider<T>
        where T : IMessage<T>, new()
    {
        private readonly string _prefix;

        public FastStoreKeyPrefixProvider(string prefix)
        {
            _prefix = prefix;
        }

        public string GetStoreKeyPrefix()
        {
            return _prefix;
        }
    }


    public abstract class KeyValueStoreBase<TKeyValueDbContext, T> : IKeyValueStore<T>
        where TKeyValueDbContext : KeyValueDbContext<TKeyValueDbContext>
        where T : class, IMessage<T>, new()
    {
        private readonly TKeyValueDbContext _keyValueDbContext;

        private readonly IKeyValueCollection _collection;

        private readonly MessageParser<T> _messageParser;


        public KeyValueStoreBase(TKeyValueDbContext keyValueDbContext, IStoreKeyPrefixProvider<T> prefixProvider)
        {
            _keyValueDbContext = keyValueDbContext;
            // ReSharper disable once VirtualMemberCallInConstructor
            _collection = keyValueDbContext.Collection(prefixProvider.GetStoreKeyPrefix());

            _messageParser = new MessageParser<T>(() => new T());
        }

        public async Task SetAsync(string key, T value)
        {
            await _collection.SetAsync(key, Serialize(value));
        }

        private static byte[] Serialize(T value)
        {
            return value?.ToByteArray();
        }

        public async Task SetAllAsync(Dictionary<string, T> pipelineSet)
        {
            await _collection.SetAllAsync(
                pipelineSet.ToDictionary(k => k.Key, v => Serialize(v.Value)));
        }

        public virtual async Task<T> GetAsync(string key)
        {
            var result = await _collection.GetAsync(key);

            return result == null ? default(T) : Deserialize(result);
        }

        private T Deserialize(byte[] result)
        {
            return _messageParser.ParseFrom(result);
        }

        public virtual async Task RemoveAsync(string key)
        {
            await _collection.RemoveAsync(key);
        }
    }
}