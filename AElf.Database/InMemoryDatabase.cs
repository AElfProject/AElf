using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AElf.Database
{
    public class InMemoryDatabase<TKeyValueDbContext> : IKeyValueDatabase<TKeyValueDbContext>
        where TKeyValueDbContext : KeyValueDbContext<TKeyValueDbContext>
    {
        private readonly ConcurrentDictionary<string, byte[]> _dictionary = new ConcurrentDictionary<string, byte[]>();

        public Task<byte[]> GetAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("key is empty");
            }

            return _dictionary.TryGetValue(key, out var value) ? Task.FromResult(value) : Task.FromResult<byte[]>(null);
        }

        public Task<bool> SetAsync(string key, byte[] bytes)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("key is empty");
            }

            _dictionary[key] = bytes;
            return Task.FromResult(true);
        }

        public Task<bool> RemoveAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("key is empty");
            }

            _dictionary.TryRemove(key, out _);
            return Task.FromResult(true);
        }

        public async Task<bool> PipelineSetAsync(Dictionary<string, byte[]> cache)
        {
            if (cache.Count == 0)
            {
                return true;
            }

            return await Task.Factory.StartNew(() =>
            {
                foreach (var change in cache)
                {
                    _dictionary[change.Key] = change.Value;
                }

                return true;
            });
        }

        public bool IsConnected()
        {
            return true;
        }
    }
}