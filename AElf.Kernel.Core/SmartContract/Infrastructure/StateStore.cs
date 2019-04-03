using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Infrastructure;
using Google.Protobuf;

namespace AElf.Kernel.SmartContract.Infrastructure
{
    public class StateStore<T> : KeyValueStoreBase<StateKeyValueDbContext, T>, IStateStore<T>
        where T : class, IMessage<T>, new()
    {
        public StateStore(StateKeyValueDbContext keyValueDbContext, IStoreKeyPrefixProvider<T> prefixProvider) : base(
            keyValueDbContext, prefixProvider)
        {
        }
    }

    public interface INotModifiedCachedStateStore<T> : IStateStore<T>
        where T : IMessage<T>, new()
    {
    }

    public class NotModifiedCachedStateStore<T> : INotModifiedCachedStateStore<T>
        where T : class, IMessage<T>, new()
    {
        private readonly IStateStore<T> _stateStoreImplementation;

        private Dictionary<string, T> _cache = new Dictionary<string, T>();
        private Queue<string> _toBeCleanedKeys = new Queue<string>();

        public NotModifiedCachedStateStore(IStateStore<T> stateStoreImplementation)
        {
            _stateStoreImplementation = stateStoreImplementation;
        }

        public async Task SetAsync(string key, T value)
        {
            await _stateStoreImplementation.SetAsync(key, value);
        }

        public async Task PipelineSetAsync(Dictionary<string, T> pipelineSet)
        {
            await _stateStoreImplementation.PipelineSetAsync(pipelineSet);
        }

        public async Task<T> GetAsync(string key)
        {
            if (_cache.TryGetValue(key, out var item))
            {
                return item;
            }

            _toBeCleanedKeys.Enqueue(key);
            while (_toBeCleanedKeys.Count > 100)
            {
                try
                {
                    _cache.Remove(_toBeCleanedKeys.Dequeue());
                }
                catch
                {
                    //ignore concurrency exceptions 
                }
            }

            return _cache[key] = await _stateStoreImplementation.GetAsync(key);
        }

        public async Task RemoveAsync(string key)
        {
            _cache.Remove(key);
            await _stateStoreImplementation.RemoveAsync(key);
        }
    }
}