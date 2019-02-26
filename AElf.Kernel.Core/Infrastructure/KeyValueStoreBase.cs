using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common.Serializers;
using AElf.Database;
using Google.Protobuf;

namespace AElf.Kernel.Infrastructure
{
    public abstract class KeyValueStoreBase<TKeyValueDbContext, T> : IKeyValueStore<T>
        where TKeyValueDbContext : KeyValueDbContext<TKeyValueDbContext>
        where T : IMessage<T>, new()
    {
        private readonly TKeyValueDbContext _keyValueDbContext;

        private readonly IKeyValueCollection _collection;

        private readonly MessageParser<T> _messageParser;

        protected abstract string GetDataPrefix();

        public KeyValueStoreBase(TKeyValueDbContext keyValueDbContext)
        {
            _keyValueDbContext = keyValueDbContext;
            // ReSharper disable once VirtualMemberCallInConstructor
            _collection = keyValueDbContext.Collection(GetDataPrefix());

            _messageParser = new MessageParser<T>(() => new T());
        }

        public async Task SetAsync(string key, T value)
        {
            await _collection.SetAsync(key, Serialize(value));
        }

        private byte[] Serialize(T value)
        {
            return value?.ToByteArray();
        }

        public async Task PipelineSetAsync(Dictionary<string, T> pipelineSet)
        {
            await _collection.PipelineSetAsync(
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